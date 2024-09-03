// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNet.Batch
{
    /// <summary>
    /// Represents an API <see cref="ChangeSet"/> request.
    /// </summary>
    public class RestierBatchChangeSetRequestItem : ChangeSetRequestItem
    {
        /// <summary>
        /// An Api.
        /// </summary>
        private readonly ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierBatchChangeSetRequestItem" /> class.
        /// </summary>
        /// <param name="api">An Api.</param>
        /// <param name="requests">The request messages.</param>
        public RestierBatchChangeSetRequestItem(ApiBase api, IEnumerable<HttpRequestMessage> requests)
            : base(requests)
        {
            Ensure.NotNull(api, nameof(api));
            this.api = api;
        }

        /// <summary>
        /// Asynchronously sends the request.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the batch response.</returns>
        public override async Task<ODataBatchResponseItem> SendRequestAsync(
            HttpMessageInvoker invoker,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(invoker, nameof(invoker));

            var changeSetProperty = new RestierChangeSetProperty(this)
            {
                ChangeSet = new ChangeSet()
            };
            SetChangeSetProperty(changeSetProperty);

            var parallelSubmit = true;
            foreach (var request in Requests)
            {
                var content = await request.Content.ReadAsStringAsync();

                // very hacky way to check if the request is a dependson request,
                // since we don't have a way to get the dependson attribute from the request
                // without parsing the content
                if (content.IndexOf("dependson", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    parallelSubmit = false;
                    break;
                }
            }

            var contentIdToLocationMapping = new ConcurrentDictionary<string, string>();
            var responseTasks = new List<Task<Task<HttpResponseMessage>>>();

            foreach (var request in Requests)
            {
                // Since exceptions may occurs before the request is sent to RestierController,
                // we must catch the exceptions here and call OnChangeSetCompleted,
                // so as to avoid deadlock mentioned in GitHub Issue #82.
                var tcs = new TaskCompletionSource<HttpResponseMessage>();
                var task = SendMessageAsync(invoker, request, cancellationToken, contentIdToLocationMapping)
                    .ContinueWith(t =>
                        {
                            if (t.Exception is not null)
                            {
                                var taskEx = (t.Exception.InnerExceptions is not null &&
                                              t.Exception.InnerExceptions.Count == 1)
                                    ? t.Exception.InnerExceptions.First()
                                    : t.Exception;
                                changeSetProperty.Exceptions.Add(taskEx);
                                changeSetProperty.OnChangeSetCompleted();
                                tcs.SetException(taskEx.Demystify());
                            }
                            else
                            {
                                tcs.SetResult(t.Result);
                            }

                            return tcs.Task;
                        },
                        cancellationToken,
                        TaskContinuationOptions.None,
                        TaskScheduler.Current);

                if (!parallelSubmit)
                {
                    await task;
                }
                
                responseTasks.Add(task);
            }

            if (parallelSubmit)
            {
                // the responseTasks will be complete after:
                // - the ChangeSet is submitted
                // - the responses are created and
                // - the controller actions have returned
                await Task.WhenAll(responseTasks).ConfigureAwait(false);
            }

            var responses = new List<HttpResponseMessage>();
            try
            {
                foreach (var responseTask in responseTasks)
                {
                    var response = responseTask.Result.Result;
                    if (response.IsSuccessStatusCode)
                    {
                        responses.Add(response);
                    }
                    else
                    {
                        DisposeResponses(responses);
                        responses.Clear();
                        responses.Add(response);
                        return new ChangeSetResponseItem(responses);
                    }
                }
            }
            catch
            {
                DisposeResponses(responses);
                throw;
            }

            return new ChangeSetResponseItem(responses);
        }

        /// <summary>
        /// Asynchronously submits a <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="changeSet">The change set to submit.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
#pragma warning disable CA1822 // Do not declare static members on generic types
        internal async Task SubmitChangeSet(ChangeSet changeSet)
#pragma warning restore CA1822 // Do not declare static members on generic types

        {
            var submitResults = await api.SubmitAsync(changeSet).ConfigureAwait(false);
        }

        private static void DisposeResponses(IEnumerable<HttpResponseMessage> responses)
        {
            foreach (var response in responses)
            {
                if (response is not null)
                {
                    response.Dispose();
                }
            }
        }

        private void SetChangeSetProperty(RestierChangeSetProperty changeSetProperty)
        {
            foreach (var request in Requests)
            {
                request.SetChangeSet(changeSetProperty);
            }
        }
    }
}
