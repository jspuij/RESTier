// <copyright file="TypeExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace System
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Extension methods on a <see cref="Type"/> instance.
    /// </summary>
    internal static partial class TypeExtensions
    {
        private const BindingFlags QualifiedMethodBindingFlags = BindingFlags.NonPublic |
                                                                 BindingFlags.Static |
                                                                 BindingFlags.Instance |
                                                                 BindingFlags.IgnoreCase |
                                                                 BindingFlags.DeclaredOnly;

        /// <summary>
        /// Find a base type or implemented interface which has a generic definition
        /// represented by the parameter, <c>definition</c>.
        /// </summary>
        /// <param name="type">
        /// The subject type.
        /// </param>
        /// <param name="definition">
        /// The generic definition to check with.
        /// </param>
        /// <returns>
        /// The base type or the interface found; otherwise, <c>null</c>.
        /// </returns>
        public static Type FindGenericType(this Type type, Type definition)
        {
            if (type == null)
            {
                return null;
            }

            // If the type conforms the given generic definition, no further check required.
            if (type.IsGenericDefinition(definition))
            {
                return type;
            }

            // If the definition is interface, we only need to check the interfaces implemented by the current type
            if (definition.IsInterface)
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.IsGenericDefinition(definition))
                    {
                        return interfaceType;
                    }
                }
            }
            else if (!type.IsInterface)
            {
                // If the definition is not an interface, then the current type cannot be an interface too.
                // Otherwise, we should only check the parent class types of the current type.

                // no null check for the type required, as we are sure it is not an interface type
                while (type != typeof(object))
                {
                    if (type.IsGenericDefinition(definition))
                    {
                        return type;
                    }

                    type = type.BaseType;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a non-public method on a type by it's qualified name.
        /// </summary>
        /// <param name="type">The type to get the method in.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <returns>A <see cref="MethodInfo"/> instance or null if the MethodInfo is not found.</returns>
        public static MethodInfo GetQualifiedMethod(this Type type, string methodName) => type.GetMethod(methodName, QualifiedMethodBindingFlags);

        /// <summary>
        /// Tries to get the element type for a type by looking for an <see cref="IEnumerable{T}"/> implementation.
        /// </summary>
        /// <remarks>Does not return char for a string.</remarks>
        /// <param name="type">The type to inspect.</param>
        /// <param name="elementType">The extracted element type.</param>
        /// <returns>True when an Element Type was found, false otherwise.</returns>
        public static bool TryGetElementType(this Type type, out Type elementType)
        {
            // Special case: string implements IEnumerable<char> however it should
            // NOT be treated as a collection type.
            if (type == typeof(string))
            {
                elementType = null;
                return false;
            }

            var interfaceType = type.FindGenericType(typeof(IEnumerable<>));
            if (interfaceType != null)
            {
                elementType = interfaceType.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool IsGenericDefinition(this Type type, Type definition)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == definition;
        }
    }
}
