// <copyright file="HttpResponseMessageExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Restier.Tests.Shared.AspNet.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Extension methods the <see cref="HttpResponseMessage" /> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Deserializes a response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="message">The <see cref="HttpResponseMessage"/> instance.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<(T Response, string ErrorContent)> DeserializeResponseAsync<T>(this HttpResponseMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!message.IsSuccessStatusCode)
            {
                return (default, await message.Content.ReadAsStringAsync().ConfigureAwait(false));
            }

            var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (JsonConvert.DeserializeObject<T>(content), null);
        }

        /// <summary>
        /// Deserializes a response using the specified settings.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="message">The <see cref="HttpResponseMessage"/> instance.</param>
        /// <param name="settings">The JSON Serializer settings.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<(T Response, string ErrorContent)> DeserializeResponseAsync<T>(this HttpResponseMessage message, JsonSerializerSettings settings)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!message.IsSuccessStatusCode)
            {
                return (default, await message.Content.ReadAsStringAsync().ConfigureAwait(false));
            }

            var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (JsonConvert.DeserializeObject<T>(content, settings), null);
        }

        /// <summary>
        /// Deserializes a response using the specified settings.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <typeparam name="TError">The error type.</typeparam>
        /// <param name="message">The <see cref="HttpResponseMessage"/> instance.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<(TResponse Response, TError ErrorContent)> DeserializeResponseAsync<TResponse, TError>(this HttpResponseMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!message.IsSuccessStatusCode)
            {
                return (default, JsonConvert.DeserializeObject<TError>(content));
            }

            return (JsonConvert.DeserializeObject<TResponse>(content), default);
        }

        /// <summary>
        /// Deserializes a response using the specified settings.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <typeparam name="TError">The error type.</typeparam>
        /// <param name="message">The <see cref="HttpResponseMessage"/> instance.</param>
        /// <param name="settings">The JSON Serializer settings.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<(TResponse Response, TError ErrorContent)> DeserializeResponseAsync<TResponse, TError>(this HttpResponseMessage message, JsonSerializerSettings settings)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!message.IsSuccessStatusCode)
            {
                return (default, JsonConvert.DeserializeObject<TError>(content));
            }

            return (JsonConvert.DeserializeObject<TResponse>(content, settings), default);
        }
    }
}
