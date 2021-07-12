// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace IssueNotificationBot.Models
{
    // Model for persistent storage that maps Teams User ID to their GitHub User ID, which is used to store the rest of the user data.
    public class TeamsUserToGitHubMap
    {
        public readonly string TeamsUserId;
        public readonly string GitHubUserLogin;

        public TeamsUserToGitHubMap(string teamsUserId, string gitHubUserLogin)
        {
            TeamsUserId = teamsUserId;
            GitHubUserLogin = gitHubUserLogin;
        }
    }
}
