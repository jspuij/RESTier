// <copyright file="ApiBaseExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Tests.Shared.ConventionDefinitions;

    /// <summary>
    /// Extension methods to <see cref="ApiBase"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ApiBaseExtensions
    {
        private const string Separator = "------------------------------------------------------------";

        /// <summary>
        /// An extension method that generates a Markdown table of all of the possible Restier methods for the given API in the first column, and a boolean
        /// indicating whether or not the method was found in the second column.
        /// </summary>
        /// <param name="api">The <see cref="ApiBase"/> instance to process.</param>
        /// <returns>A string containing the Markdown table of results.</returns>
        public static async Task<string> GenerateVisibilityMatrix(this ApiBase api)
        {
            var sb = new StringBuilder();
            var model = (EdmModel)await api.GetModelAsync(default);
            var apiType = api.GetType();

            var conventions = model.GenerateConventionDefinitions();
            var entitySetMatrix = conventions.OfType<RestierConventionEntitySetDefinition>().ToDictionary(c => c, c => false);
            var methodMatrix = conventions.OfType<RestierConventionMethodDefinition>().ToDictionary(c => c, c => false);

            foreach (var definition in entitySetMatrix.ToList())
            {
                var value = false;
                switch (definition.Key.PipelineState)
                {
                    case RestierPipelineState.Authorization:
                        value = IsAuthorizerMethodAccessible(apiType, definition.Key.Name);
                        break;
                    default:
                        if (definition.Key.EntitySetOperation == RestierEntitySetOperation.Filter)
                        {
                            value = IsFilterMethodAccessible(apiType, definition.Key.Name);
                        }
                        else
                        {
                            value = IsInterceptorMethodAccessible(apiType, definition.Key.Name);
                        }

                        break;
                }

                entitySetMatrix[definition.Key] = value;
            }

            foreach (var definition in methodMatrix.ToList())
            {
                var value = false;
                switch (definition.Key.PipelineState)
                {
                    case RestierPipelineState.Authorization:
                        value = IsAuthorizerMethodAccessible(apiType, definition.Key.Name);
                        break;
                    default:
                        value = IsInterceptorMethodAccessible(apiType, definition.Key.Name);
                        break;
                }

                methodMatrix[definition.Key] = value;
            }

            sb.AppendLine(Separator);
            sb.AppendLine(string.Format("{0,-50} | {1,7}", "Function Name", "Found?"));
            sb.AppendLine(Separator);
            foreach (var result in entitySetMatrix)
            {
                sb.AppendLine(string.Format("{0,-50} | {1,7}", result.Key.Name, result.Value));
            }

            foreach (var result in methodMatrix)
            {
                sb.AppendLine(string.Format("{0,-50} | {1,7}", result.Key.Name, result.Value));
            }

            sb.AppendLine(Separator);

            return sb.ToString();
        }

        /// <summary>
        /// An extension method that generates the Visibility Matrix for the current Api and writes it to a text file.
        /// </summary>
        /// <param name="api">The <see cref="ApiBase"/> instance to build the Visibility Matrix for.</param>
        /// <param name="sourceDirectory">
        /// A string containing the relative or absolute path to use as the root. The default is "". If you want to be able to have it as part of the project,
        /// so you can check it into source control, use "..//..//".
        /// </param>
        /// <param name="suffix">A string to append to the Api name when writing the text file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task WriteCurrentVisibilityMatrix(this ApiBase api, string sourceDirectory = "", string suffix = "ApiSurface")
        {
            var filePath = $"{sourceDirectory}{api.GetType().Name}-{suffix}.txt";
            var report = await api.GenerateVisibilityMatrix();
            System.IO.File.WriteAllText(filePath, report);
        }

        /// <summary>
        /// This method recreates parts of the code in <see cref="ConventionBasedChangeSetItemAuthorizer" /> to determine if
        /// Restier can access the specified method name in the specified type.
        /// </summary>
        /// <param name="api">The api.</param>
        /// <param name="methodName">The method name.</param>
        /// <returns>A bool indicating whether the method is accessible.</returns>
        private static bool IsAuthorizerMethodAccessible(Type api, string methodName)
        {
            var returnType = typeof(bool);
            var method = api.GetQualifiedMethod(methodName);

            if (method != null && (method.IsFamily || method.IsFamilyOrAssembly) &&
                method.ReturnType == returnType)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method recreates parts of the code in <see cref="ConventionBasedChangeSetItemFilter" /> to determine if
        /// Restier can access the specified method name in the specified type.
        /// </summary>
        /// <param name="api">The api.</param>
        /// <param name="methodName">The method name.</param>
        /// <returns>True when accessible, false otherwise.</returns>
        private static bool IsInterceptorMethodAccessible(Type api, string methodName)
        {
            var method = api.GetQualifiedMethod(methodName);

            if (method != null &&
                (method.ReturnType == typeof(void) ||
                typeof(Task).IsAssignableFrom(method.ReturnType)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method recreates parts of the code in <see cref="ConventionBasedQueryExpressionProcessor" /> to determine if
        /// Restier can access the specified method name in the specified type.
        /// </summary>
        /// <param name="api">The api.</param>
        /// <param name="methodName">The method name.</param>
        /// <returns>True when accessible, false otherwise.</returns>
        private static bool IsFilterMethodAccessible(Type api, string methodName)
        {
            var method = api.GetQualifiedMethod(methodName);

            if (method != null && (method.IsFamily || method.IsFamilyOrAssembly))
            {
                var parameter = method.GetParameters().SingleOrDefault();
                if (parameter != null &&
                    parameter.ParameterType == method.ReturnType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}