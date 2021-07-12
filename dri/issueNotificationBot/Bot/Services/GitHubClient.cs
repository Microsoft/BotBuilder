// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Models;
using Microsoft.Rest.Serialization;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IssueNotificationBot.Services
{
    public class GitHubClient
    {
        private readonly string _token;
        private readonly HttpClient _client;

        public GitHubClient(string token)
        {
            _token = token;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"token {_token}");
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.UserAgent);
        }

        /// <summary>
        /// Get GitHub user info for the user that has authenticated with the bot via OAuth.
        /// </summary>
        public async Task<GitHubUserResponse> GetAuthenticatedUser()
        {
            return await GetJsonResponseAsObject<GitHubUserResponse>(Constants.GitHubApiAuthenticatedUserPath).ConfigureAwait(false);
        }

        private async Task<T> GetJsonResponseAsObject<T>(string path)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(Constants.GitHubApiBaseUrl + path)
            };
            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            return SafeJsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
        }
    }
}
