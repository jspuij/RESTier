// <copyright file="ConventionBasedOperationAuthorizer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Core
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Restier.Core.Operation;

    /// <summary>
    /// A convention-based operation authorizer.
    /// </summary>
    public class ConventionBasedOperationAuthorizer : IOperationAuthorizer
    {
        private Type targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedOperationAuthorizer"/> class.
        /// </summary>
        /// <param name="targetType">The target type to check for authorizer functions.</param>
        public ConventionBasedOperationAuthorizer(Type targetType)
        {
            Ensure.NotNull(targetType, nameof(targetType));
            this.targetType = targetType;
        }

        /// <inheritdoc/>
        public Task<bool> AuthorizeAsync(OperationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            var result = true;

            var returnType = typeof(bool);
            var methodName = ConventionBasedMethodNameFactory.GetFunctionMethodName(context, RestierPipelineState.Authorization, RestierOperationMethod.Execute);
            var method = this.targetType.GetQualifiedMethod(methodName);

            if (method == null)
            {
                return Task.FromResult(result);
            }

            if (!method.IsFamily && !method.IsFamilyOrAssembly)
            {
                Trace.WriteLine($"Restier Authorizer found '{methodName}' but it is inaccessible due to its protection level. Your method will not be called until you change it to 'protected internal'.");
                return Task.FromResult(result);
            }

            if (method.ReturnType != returnType)
            {
                Trace.WriteLine($"Restier Authorizer found '{methodName}' but it does not return a boolean value. Your method will not be called until you correct the return type.");
                return Task.FromResult(result);
            }

            object target = null;
            if (!method.IsStatic)
            {
                target = context.Api;
                if (!this.targetType.IsInstanceOfType(target))
                {
                    Trace.WriteLine("The Restier API is of the incorrect type.");
                    return Task.FromResult(result);
                }
            }

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                result = (bool)method.Invoke(target, null);
            }

            Trace.WriteLine($"Restier Authorizer found '{methodName}', but it has an incorrect number of arguments. The number of arguments should be 0.");
            return Task.FromResult(result);
        }
    }
}
