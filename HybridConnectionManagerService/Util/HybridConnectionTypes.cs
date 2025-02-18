//------------------------------------------------------------------------------
// <copyright file="ResponseMessageEnvelope.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using Azure.ResourceManager.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HybridConnectionManager.Service
{
    /// <summary>
    /// Message envelope that contains the common Azure resource manager properties and the resource provider specific content.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AzureResponseEnvelope<T>
    {
        /// <summary>
        /// Resource Id. Typically ID is populated only for responses to GET requests. Caller is responsible for passing in this
        /// value for GET requests only.
        /// For example: /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupId}/providers/Microsoft.Web/sites/{sitename}
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of resource e.g "Microsoft.Web/sites".
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>
        /// Kind of app e.g web app, api app, mobile app.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Kind { get; set; }

        /// <summary>
        /// Geographical region resource belongs to e.g. SouthCentralUS, SouthEastAsia.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Location { get; set; }

        /// <summary>
        /// Tags associated with resource.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Resource specific properties.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public T Properties { get; set; }
    }

    public class AzureListResponseEnvelope<T>
    {
        public List<AzureResponseEnvelope<T>> Value { get; set; }
    }

    public class AuthorizationRules
    {
        public List<string> Rights { get; set; }
    }

    public class AuthorizationRuleKeys
    {
        public string PrimaryConnectionString { get; set; }
        public string SecondaryConnectionString { get; set; }
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
        public string KeyName { get; set; }
    }
}