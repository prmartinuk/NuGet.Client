﻿using Newtonsoft.Json.Linq;
using NuGet.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public class V3RawSearchResource : RawSearchResource
    {

        private readonly DataClient _client;
        private readonly Uri[] _searchEndpoints;

        public V3RawSearchResource(DataClient client, IEnumerable<Uri> searchEndpoints)
            : base()
        {
            _client = client;
            _searchEndpoints = searchEndpoints.ToArray();
        }

        public override async Task<JObject> SearchPage(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            for (int i = 0; i < _searchEndpoints.Length; i++)
            {
                var endpoint = _searchEndpoints[i];

                var queryUrl = new UriBuilder(endpoint.AbsoluteUri);
                string queryString =
                    "q=" + searchTerm +
                    "&skip=" + skip.ToString() +
                    "&take=" + take.ToString() +
                    "&includePrerelease=" + filters.IncludePrerelease.ToString().ToLowerInvariant();
                if (filters.IncludeDelisted)
                {
                    queryString += "&includeDelisted=true";
                }

                if (filters.SupportedFrameworks != null && filters.SupportedFrameworks.Any())
                {
                    string frameworks =
                        String.Join("&",
                            filters.SupportedFrameworks.Select(
                                fx => "supportedFramework=" + fx.ToString()));
                    queryString += "&" + frameworks;
                }
                queryUrl.Query = queryString;

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        JObject searchJson = await _client.GetJObjectAsync(queryUrl.Uri, cancellationToken);

                        if (searchJson != null)
                        {
                            return searchJson;
                        }
                    }
                    catch (Exception)
                    {
                        Debug.Fail("Search failed");

                        if (i == _searchEndpoints.Length - 1)
                        {
                            // throw on the last one
                            throw;
                        }
                    }
                }
            }

            // TODO: get a better message for this
            throw new NuGetProtocolException(Strings.Protocol_MissingSearchService);
        }

        public override async Task<IEnumerable<JObject>> Search(string searchTerm, SearchFilter filters, int skip, int take, CancellationToken cancellationToken)
        {
            var results = await SearchPage(searchTerm, filters, skip, take, cancellationToken);

            var data = results.Value<JArray>("data");
            if (data == null)
            {
                return Enumerable.Empty<JObject>();
            }

            return data.Select(e => e as JObject).Where(e => e != null);
        }
    }
}
