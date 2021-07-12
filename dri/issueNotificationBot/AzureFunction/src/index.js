const { GitHubService } = require('./services');
const { GitHub } = require('./config');

module.exports = async function(context) {
  context.log('Starting Processes');
  const gitHub = new GitHubService(GitHub, context);
  
  context.log('Processing Issues');
  await gitHub.processIssues();
  context.log('Done Processing Issues');

  if (shouldProcessPRs()) {
    context.log('Processing PRs');
    await gitHub.processPRs();
    context.log('Done processing PRs');
  }

  context.log('Finished Processes');
};

function shouldProcessPRs() {
  const now = new Date();

  const day = now.getDay();
  const earliest = new Date().setHours(7, 55);
  const latest = new Date().setHours(8, 10);

  // if weekday
  if (day > 0 && day < 6 &&
    // Function runs every half hour, we want to make sure we capture it when it does
    now > earliest && now < latest) {
      return true;
  }

  return false;
}
