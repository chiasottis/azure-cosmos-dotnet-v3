﻿//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeed.LeaseManagement
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeed.Utils;
    using Newtonsoft.Json;

    /// <summary>
    /// Implementation of <see cref="DocumentServiceLeaseStore"/> for state in Azure Cosmos DB
    /// </summary>
    internal sealed class DocumentServiceLeaseStoreCosmos : DocumentServiceLeaseStore
    {
        private readonly CosmosContainer container;
        private readonly string containerNamePrefix;
        private readonly RequestOptionsFactory requestOptionsFactory;
        private string lockETag;

        public DocumentServiceLeaseStoreCosmos(
            CosmosContainer container,
            string containerNamePrefix,
            RequestOptionsFactory requestOptionsFactory)
        {
            this.container = container;
            this.containerNamePrefix = containerNamePrefix;
            this.requestOptionsFactory = requestOptionsFactory;
        }

        public override async Task<bool> IsInitializedAsync()
        {
            string markerDocId = this.GetStoreMarkerName();

            return await this.container.ItemExistsAsync(this.requestOptionsFactory.GetPartitionKey(markerDocId), markerDocId).ConfigureAwait(false);
        }

        public override async Task MarkInitializedAsync()
        {
            string markerDocId = this.GetStoreMarkerName();
            var containerDocument = new { id = markerDocId };
            await this.container.Items.CreateItemAsync<dynamic>(                
                containerDocument,
                new ItemRequestOptions { PartitionKey = this.requestOptionsFactory.GetPartitionKey(markerDocId) }
                ).ConfigureAwait(false);
        }

        public override async Task<bool> AcquireInitializationLockAsync(TimeSpan lockTime)
        {
            string lockId = this.GetStoreLockName();
            var containerDocument = new LockDocument (){ Id = lockId, TimeToLive = (int)lockTime.TotalSeconds };
            var document = await this.container.TryCreateItemAsync<LockDocument>(
                this.requestOptionsFactory.GetPartitionKey(lockId),
                containerDocument).ConfigureAwait(false);

            if (document != null)
            {
                this.lockETag = document.ETag;
                return true;
            }

            return false;
        }

        public override async Task<bool> ReleaseInitializationLockAsync()
        {
            string lockId = this.GetStoreLockName();
            var requestOptions = new ItemRequestOptions()
            {
                AccessCondition = new AccessCondition { Type = AccessConditionType.IfMatch, Condition = this.lockETag }
            };

            var document = await this.container.TryDeleteItemAsync<LockDocument>(
                this.requestOptionsFactory.GetPartitionKey(lockId),
                lockId,
                requestOptions).ConfigureAwait(false);

            if (document != null)
            {
                this.lockETag = null;
                return true;
            }

            return false;
        }

        private string GetStoreMarkerName()
        {
            return this.containerNamePrefix + ".info";
        }

        private string GetStoreLockName()
        {
            return this.containerNamePrefix + ".lock";
        }

        private class LockDocument
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("ttl")]
            public int TimeToLive { get; set; }
        }
    }
}