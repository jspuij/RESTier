// <copyright file="ConventionBasedChangeSetItemFilter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Restier.Core.Submit;

    /// <summary>
    /// A convention-based change set item processor which calls logic like OnInserting and OnInserted.
    /// </summary>
    public class ConventionBasedChangeSetItemFilter : IChangeSetItemFilter
    {
        private Type targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedChangeSetItemFilter"/> class.
        /// </summary>
        /// <param name="targetType">The target type to check for filter functions.</param>
        public ConventionBasedChangeSetItemFilter(Type targetType)
        {
            Ensure.NotNull(targetType, nameof(targetType));
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public Task OnChangeSetItemProcessingAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            Ensure.NotNull(item, nameof(item));
            Ensure.NotNull(context, nameof(context));
            return this.InvokeProcessorMethodAsync(context, item, RestierPipelineState.PreSubmit);
        }

        /// <inheritdoc/>
        public Task OnChangeSetItemProcessedAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            Ensure.NotNull(item, nameof(item));
            Ensure.NotNull(context, nameof(context));
            return this.InvokeProcessorMethodAsync(context, item, RestierPipelineState.PostSubmit);
        }

        private static object[] GetParameters(ChangeSetItem item)
        {
            switch (item.Type)
            {
                case ChangeSetItemType.DataModification:
                    var dataModification = (DataModificationItem)item;
                    return new object[] { dataModification.Resource };

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidChangeSetEntryType, item.Type));
            }
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] parameters)
        {
            return methodParameters.Length == parameters.Length && !methodParameters.Where((mp, i) => !mp.ParameterType.IsInstanceOfType(parameters[i])).Any();
        }

        private Task InvokeProcessorMethodAsync(SubmitContext context, ChangeSetItem item, RestierPipelineState pipelineState)
        {
            var dataModification = (DataModificationItem)item;
            var expectedMethodName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(dataModification, pipelineState);
            var expectedMethod = this.targetType.GetQualifiedMethod(expectedMethodName);

            if (expectedMethod == null)
            {
                var actualMethodName = expectedMethodName.Replace(dataModification.ExpectedResourceType.Name, dataModification.ResourceSetName);
                var actualMethod = this.targetType.GetQualifiedMethod(actualMethodName);
                if (actualMethod != null)
                {
                    Trace.WriteLine($"Restier Filter expected'{expectedMethodName}' but found '{actualMethodName}'. Your method will not be called until you correct the method name.");
                }

                return Task.CompletedTask;
            }

            if (!expectedMethod.IsFamily && !expectedMethod.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier Filter found '{expectedMethod}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                return Task.CompletedTask;
            }

            if (expectedMethod.ReturnType != typeof(void) && !typeof(Task).IsAssignableFrom(expectedMethod.ReturnType))
            {
                Trace.WriteLine($"Restier Filter found '{expectedMethod}' but it does not return void or a Task. Your method will not be called until you correct the return type.");
                return Task.CompletedTask;
            }

            object target = null;
            if (!expectedMethod.IsStatic)
            {
                target = context.Api;
                if (target == null || !this.targetType.IsInstanceOfType(target))
                {
                    Trace.WriteLine("The Restier API is of the incorrect type.");
                    return Task.CompletedTask;
                }
            }

            var parameters = GetParameters(item);
            var methodParameters = expectedMethod.GetParameters();
            if (ParametersMatch(methodParameters, parameters))
            {
                var result = expectedMethod.Invoke(target, parameters);
                if (result is Task resultTask)
                {
                    return resultTask;
                }
            }

            Trace.WriteLine($"Restier Authorizer found '{expectedMethod}', but it has an incorrect number of arguments or the types don't match. The number of arguments should be 1.");
            return Task.CompletedTask;
        }
    }
}
