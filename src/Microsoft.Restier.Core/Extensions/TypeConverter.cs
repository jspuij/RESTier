// <copyright file="TypeConverter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace System
{
    /// <summary>
    /// Type Converter helper methods.
    /// </summary>
    internal static class TypeConverter
    {
        /// <summary>
        /// Calls <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/>, but handles
        /// DateTimeOffset as well.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="conversionType">The type to convert to.</param>
        /// <param name="provider">The <see cref="IFormatProvider"/> to use.</param>
        /// <returns>The converted type.</returns>
        public static object ChangeType(object value, Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(DateTime) && value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).DateTime;
            }

            return Convert.ChangeType(value, conversionType, provider);
        }
    }
}
