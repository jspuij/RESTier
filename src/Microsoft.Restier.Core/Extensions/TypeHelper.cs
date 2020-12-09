// <copyright file="TypeHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace System
{
    /// <summary>
    /// Static helper methods for a Type.
    /// </summary>
    internal static class TypeHelper
    {
        /// <summary>
        /// Gets the underlying type in case of a Nullable or the type itself.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The underlying type or itself.</returns>
        public static Type GetUnderlyingTypeOrSelf(Type type) => Nullable.GetUnderlyingType(type) ?? type;

        /// <summary>
        /// Tests Whether the type or underlying type is an Enum.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type is an Enum, false otherwise.</returns>
        public static bool IsEnum(Type type)
        {
            var underlyingTypeOrSelf = GetUnderlyingTypeOrSelf(type);
            return underlyingTypeOrSelf.IsEnum;
        }

        /// <summary>
        /// Tests Whether the type or underlying type is an DateTime.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type is an DateTime, false otherwise.</returns>
        public static bool IsDateTime(Type type)
        {
            var underlyingTypeOrSelf = GetUnderlyingTypeOrSelf(type);
            return underlyingTypeOrSelf == typeof(DateTime);
        }

        /// <summary>
        /// Tests Whether the type or underlying type is an TimeSpan.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type is an TimeSpan, false otherwise.</returns>
        public static bool IsTimeSpan(Type type)
        {
            var underlyingTypeOrSelf = GetUnderlyingTypeOrSelf(type);
            return underlyingTypeOrSelf == typeof(TimeSpan);
        }

        /// <summary>
        /// Tests Whether the type or underlying type is an DateTimeOffset.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type is an DateTimeOffset, false otherwise.</returns>
        public static bool IsDateTimeOffset(Type type)
        {
            var underlyingTypeOrSelf = GetUnderlyingTypeOrSelf(type);
            return underlyingTypeOrSelf == typeof(DateTimeOffset);
        }
    }
}
