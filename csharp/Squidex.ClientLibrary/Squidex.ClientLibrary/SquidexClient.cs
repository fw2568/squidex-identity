﻿// ==========================================================================
//  SquidexClient.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace Squidex.ClientLibrary
{
    public sealed class SquidexClient<TEntity, TData> where TData : class, new() where TEntity : SquidexEntityBase<TData>
    {
        private readonly string applicationName;
        private readonly Uri serviceUrl;
        private readonly string schemaName;
        private readonly IAuthenticator authenticator;

        public SquidexClient(Uri serviceUrl, string applicationName, string schemaName, IAuthenticator authenticator)
        {
            Guard.NotNull(serviceUrl, nameof(serviceUrl));
            Guard.NotNull(authenticator, nameof(authenticator));
            Guard.NotNullOrEmpty(schemaName, nameof(schemaName));
            Guard.NotNullOrEmpty(applicationName, nameof(applicationName));

            this.serviceUrl = serviceUrl;
            this.schemaName = schemaName;
            this.authenticator = authenticator;
            this.applicationName = applicationName;
        }

        public async Task<SquidexEntities<TEntity, TData>> GetAsync(long? skip = null, long? top = null, string filter = null, string orderBy = null, string search = null)
        {
            var queries = new List<string>();

            if (skip.HasValue)
            {
                queries.Add($"$skip={skip.Value}");
            }

            if (top.HasValue)
            {
                queries.Add($"$top={top.Value}");
            }

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                queries.Add($"$orderby={orderBy}");
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                queries.Add($"$search={search}");
            }
            else if (!string.IsNullOrWhiteSpace(filter))
            {
                queries.Add($"$filter={filter}");
            }

            var query = string.Join("&", queries);

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = "?" + query;
            }

            var response = await RequestAsync(HttpMethod.Get, query);

            return await response.Content.ReadAsJsonAsync<SquidexEntities<TEntity, TData>>();
        }

        public async Task<TEntity> GetAsync(string id)
        {
            Guard.NotNullOrEmpty(id, nameof(id));
;
            var response = await RequestAsync(HttpMethod.Get, $"{id}/");

            return await response.Content.ReadAsJsonAsync<TEntity>();
        }

        public async Task<TEntity> CreateAsync(string id, TData data)
        {
            Guard.NotNull(data, nameof(data));
            Guard.NotNullOrEmpty(id, nameof(id));

            var response = await RequestAsync(HttpMethod.Post, $"{id}/", data.ToContent());

            return await response.Content.ReadAsJsonAsync<TEntity>();
        }

        public Task UpdateAsync(string id, TData data)
        {
            Guard.NotNull(data, nameof(data));
            Guard.NotNullOrEmpty(id, nameof(id));

            return RequestAsync(HttpMethod.Put, $"{id}/", data.ToContent());
        }

        public async Task UpdateAsync(TEntity entity)
        {
            Guard.NotNull(entity, nameof(entity));

            await UpdateAsync(entity.Id, entity.Data);

            entity.MarkAsUpdated();
        }

        public Task PublishAsync(string id)
        {
            Guard.NotNullOrEmpty(id, nameof(id));

            return RequestAsync(HttpMethod.Put, $"{id}/publish/");
        }

        public async Task PublishAsync(TEntity entity)
        {
            Guard.NotNull(entity, nameof(entity));

            await PublishAsync(entity.Id);

            entity.MarkAsUpdated();
        }

        public Task UnpublishAsync(string id)
        {
            Guard.NotNullOrEmpty(id, nameof(id));

            return RequestAsync(HttpMethod.Put, $"{id}/unpublish/");
        }

        public async Task UnpublishAsync(TEntity entity)
        {
            Guard.NotNull(entity, nameof(entity));

            await UnpublishAsync(entity.Id);

            entity.MarkAsUpdated();
        }

        public Task ArchiveAsync(string id)
        {
            Guard.NotNullOrEmpty(id, nameof(id));

            return RequestAsync(HttpMethod.Put, $"{id}/archive/");
        }

        public async Task ArchiveAsync(TEntity entity)
        {
            Guard.NotNull(entity, nameof(entity));

            await ArchiveAsync(entity.Id);

            entity.MarkAsUpdated();
        }

        public Task RestoreAsync(string id)
        {
            Guard.NotNullOrEmpty(id, nameof(id));

            return RequestAsync(HttpMethod.Put, $"{id}/restore/");
        }

        public async Task RestoreAsync(TEntity entity)
        {
            Guard.NotNull(entity, nameof(entity));

            await RestoreAsync(entity.Id);

            entity.MarkAsUpdated();
        }

        public Task DeleteAsync(string id)
        {
            Guard.NotNullOrEmpty(id, nameof(id));
            
            return RequestAsync(HttpMethod.Delete, $"{id}/");
        }

        public async Task DeleteAsync(TEntity entity)
        {
            Guard.NotNull(entity, nameof(entity));

            await DeleteAsync(entity.Id);

            entity.MarkAsUpdated();
        }

        private static async Task EnsureResponseIsValidAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "Squidex API failed with internal error.";
                }
                else
                {
                    message = $"Squidex Request failed: {message}";
                }

                throw new SquidexException(message);
            }
        }

        private async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path = "", HttpContent content = null)
        {
            var uri = new Uri(serviceUrl, $"api/content/{applicationName}/{schemaName}/{path}");

            var request = new HttpRequestMessage(method, uri);

            if (content != null)
            {
                request.Content = content;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await authenticator.GetBearerTokenAsync());

            var response = await SquidexHttpClient.Instance.SendAsync(request);

            await EnsureResponseIsValidAsync(response);

            return response;
        }
    }
}
