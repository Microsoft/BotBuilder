// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using IssueNotificationBot.Models;
using IssueNotificationBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace IssueNotificationBot
{
    // This ASP Controller handles data sent from the Azure Function that polls the GitHub issues.
    // It requires that the Azure Function uses the bot AppId and AppPassword
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        private readonly GitHubIssueProcessor GitHubIssueProcessor;
        private readonly GitHubPRProcessor GitHubPRProcessor;
        private readonly ILogger Logger;
        private readonly SimpleCredentialProvider Credentials;

        public DataController(IConfiguration configuration, GitHubIssueProcessor gitHubIssueProcessor, GitHubPRProcessor gitHubPRProcessor, ILogger<DataController> logger)
        {
            Configuration = configuration;
            GitHubIssueProcessor = gitHubIssueProcessor;
            GitHubPRProcessor = gitHubPRProcessor;
            Logger = logger;
            Credentials = new SimpleCredentialProvider(Configuration["MicrosoftAppId"], Configuration["MicrosoftAppPassword"]);
        }

        [HttpPost("/api/issues")]
        public async Task<HttpStatusCode> PostIssuesAsync()
        {
            Logger.LogInformation("Received post on /api/issues");
            using var reader = new System.IO.StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync().ConfigureAwait(true);

            try
            {
                if (await IsAuthenticatedAsync(Request))
                {
                    var issues = SafeJsonConvert.DeserializeObject<GitHubIssues>(json);
                    await GitHubIssueProcessor.ProcessIssues(issues);

                    Logger.LogInformation("Finished processing post on /api/issues");
                    return HttpStatusCode.OK;
                }

                return HttpStatusCode.Forbidden;
            }
            catch (JsonException)
            {
                Logger.LogError("Received invalid data on /api/issues");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Invalid AppId"))
                {
                    return HttpStatusCode.Forbidden;
                }

                Logger.LogError($"Something went wrong in /api/issues controller: {e.Message}");
            }
            return HttpStatusCode.BadRequest;
        }

        [HttpPost("/api/prs")]
        public async Task<HttpStatusCode> PostPRsAsync()
        {
            Logger.LogInformation("Received post on /api/prs");
            using var reader = new System.IO.StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync().ConfigureAwait(true);

            try
            {
                if (await IsAuthenticatedAsync(Request))
                {
                    var prs = SafeJsonConvert.DeserializeObject<Dictionary<string, GitHubPRReviewer>>(json);
                    await GitHubPRProcessor.ProcessPRs(prs);

                    Logger.LogInformation("Finished processing post on /api/prs");
                    return HttpStatusCode.OK;
                }

                return HttpStatusCode.Forbidden;
            }
            catch (JsonException)
            {
                Logger.LogError("Received invalid data on /api/prs");
            }
            catch (Exception e)
            {
                Logger.LogError($"Something went wrong in /api/prs controller: {e.Message}");
            }
            return HttpStatusCode.BadRequest;
        }

        // Validates whether or not the Authorization Header contains a valid token based on this bot's AppId/Password.
        private async Task<bool> IsAuthenticatedAsync(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            request.Headers.TryGetValue("Authorization", out StringValues authHeader);
            var result = await JwtTokenValidation.ValidateAuthHeader(authHeader, Credentials, new SimpleChannelProvider(), Channels.Directline);

            return result.IsAuthenticated;
        }
    }
}
