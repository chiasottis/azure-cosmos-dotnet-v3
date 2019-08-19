﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Newtonsoft.Json;

    internal class QueryOperationStatistics : BaseStatistics
    {
        public QueryOperationStatistics(IReadOnlyDictionary<string, QueryMetrics> queryMetrics = null)
        {
            this.queryMetrics = queryMetrics;
        }

        public IReadOnlyDictionary<string, QueryMetrics> queryMetrics { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}