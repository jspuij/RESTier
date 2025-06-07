// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.Restier.AspNetCore.Batch
{
    /// <summary>
    /// Default implementation of <see cref="ODataBatchHandler"/> in RESTier.
    /// </summary>
    public class RestierBatchHandler : DefaultODataBatchHandler
    {
        /// <summary>
        /// Asynchronously parses the batch requests.
        /// </summary>
        /// <param name="context">The HTTP context that contains the batch requests.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpContext context)
        {
            Ensure.NotNull(context, nameof(context));

            HttpRequest request = context.Request;
            IServiceProvider requestContainer = request.CreateRouteServices(PrefixName);
            requestContainer.GetRequiredService<ODataMessageReaderSettings>().BaseUri = GetBaseUri(request);

            // TODO: JWS: needs to be a constructor dependency probably, but that's impossible now.
            var api = requestContainer.GetRequiredService<ApiBase>();

            using var reader = request.GetODataMessageReader(requestContainer);

            CancellationToken cancellationToken = context.RequestAborted;
            var requests = new List<ODataBatchRequestItem>();
            var batchReader = await reader.CreateODataBatchReaderAsync().ConfigureAwait(false);
            var batchId = Guid.NewGuid();
            IDictionary<string, string> contentToLocationMapping = new ConcurrentDictionary<string, string>();

            while (await batchReader.ReadAsync().ConfigureAwait(false))
            {
                if (batchReader.State == ODataBatchReaderState.ChangesetStart)
                {
                    IList<HttpContext> changeSetContexts = await batchReader.ReadChangeSetRequestAsync(context, batchId, cancellationToken).ConfigureAwait(false);
                    foreach (HttpContext changeSetContext in changeSetContexts)
                    {
                        // changeSetContext.Request.CopyBatchRequestProperties(context.Request);
                        changeSetContext.Request.ClearRouteServices();
                    }

                    ChangeSetRequestItem requestItem = CreateRestierBatchChangeSetRequestItem(api, changeSetContexts);
                    requestItem.ContentIdToLocationMapping = contentToLocationMapping;
                    requests.Add(requestItem);
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    // JWS: TODO: Is this correct? Shouldn't we use the api to send the operation requests to?
                    HttpContext operationContext = await batchReader.ReadOperationRequestAsync(context, batchId, cancellationToken).ConfigureAwait(false);
                    // operationContext.Request.CopyBatchRequestProperties(context.Request);
                    operationContext.Request.ClearRouteServices();
                    OperationRequestItem requestItem = new OperationRequestItem(operationContext);
                    requestItem.ContentIdToLocationMapping = contentToLocationMapping;
                    requests.Add(requestItem);
                }
            }

            return requests;
        }

        /// <summary>
        /// Creates the <see cref="RestierBatchChangeSetRequestItem"/> instance.
        /// </summary>
        /// <param name="api">A reference to the Api.</param>
        /// <param name="changeSetContexts">The list of changeset contexts.</param>
        /// <returns>The created <see cref="RestierBatchChangeSetRequestItem"/> instance.</returns>
        protected virtual RestierBatchChangeSetRequestItem CreateRestierBatchChangeSetRequestItem(ApiBase api, IList<HttpContext> changeSetContexts)
            => new RestierBatchChangeSetRequestItem(api, changeSetContexts);
    }
}
