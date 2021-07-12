const globallyTrackedLabels = [
  'customer-reported'
]

const globallyIgnoredLabels = [
  'ExemptFromDailyDRIReport'
]

const repositories = [
  { org: "Microsoft", repo: "botbuilder-azure", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-cognitiveservices", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-dotnet", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-java", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-js", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-python", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-samples", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-tools", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botbuilder-v3", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-emulator", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-directlinejs", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-solutions", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-services", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-sdk", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-composer", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-cli", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "Microsoft", repo: "botframework-webchat", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
  { org: "MicrosoftDocs", repo: "bot-docs", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels },
]

if (process.env.UseTestRepo === 'true') repositories.push({ org: "mdrichardson", repo: "testRepoForIssueNotificationBot", labels: globallyTrackedLabels, ignoreLabels: globallyIgnoredLabels });

module.exports.GitHub = {
  repositories,
  source: 'GitHub'
}
