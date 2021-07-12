// Go here for help creating/editing query: https://developer.github.com/v4/explorer/
function getPRQuery(search) {
  return {
      query:
      `{
        search(query: "${ search }", type: ISSUE, first: 100) {
          edges {
            node {
              ... on PullRequest {
                assignees(first: 100) {
                  nodes {
                    login
                    name
                    url
                  }
                }
                author {
                  ... on User {
                    login
                    name
                    url
                  }
                }
                body
                createdAt
                number
                repository {
                  name
                  url
                }
                reviewRequests(first: 10) {
                  nodes {
                    requestedReviewer {
                      ... on User {
                        login
                        name
                        url
                      }
                      ... on Team {
                        name
                        url
                        members(first: 50) {
                          nodes {
                            name
                            login
                            url
                          }
                        }
                      }
                    }
                  }
                }
                state
                url
                title
                isDraft
                reviews(first: 50) {
                  edges {
                    node {
                      state
                      author {
                        ... on User {
                          login
                          name
                          url
                        }
                      }
                    }
                  }
                }
                reviewDecision
                mergeable
                labels(first: 10) {
                  nodes {
                      url
                      name
                  }
                }
              }
            }
          }
        }
      }`
  };
}

function groupPRs(prArray) {
  const prsByReviewer = {};

  prArray.forEach((pr) => {
    if (prWaitingOnReviewer(pr)) {
      const singles = [];
      const groupMembers = new Set();
      const approvals = new Set();
      const prReviewers = new Set();

      // Track reviewers who have already approved, so we don't notify them
      pr.reviews.forEach((review) => {
        if (review.state === 'APPROVED') {
          approvals.add(review.author.login);
        }
      });

      // Iterate over requested reviewers
      pr.reviewRequests.forEach((reviewer) => {
        reviewer = reviewer.requestedReviewer;
        
        // If reviewRequest is a group, like `bf-adaptive`, it will have `members`
        if (reviewer.members) {
          reviewer.members.forEach((member) => {
            
            // For testing purposes
            if (process.env.SendAllTo && process.env.SendAllTo.length) {
              member.login = process.env.SendAllTo;
            }

            groupMembers.add(member.login);

            // Don't add the reviewer if they've already approved
            // Don't add the reviewer if this PR has already been added for them,
            //   which can happen if they're in multiple groups
            if (!approvals.has(member.login) && !prReviewers.has(member.login)) {
              prReviewers.add(member.login);
              const newPR = { ...pr, reviewingForGroup: reviewer.name };

              if (!prsByReviewer[member.login]) {
                prsByReviewer[member.login] = {
                  single: [],
                  group: [newPR]
                };
              } else {
                prsByReviewer[member.login].group.push(newPR); 
              }
            }            
          });
        // reviewRequest is an individual
        } else {

          // For testing purposes
          if (process.env.SendAllTo && process.env.SendAllTo.length) {
            reviewer.login = process.env.SendAllTo;
          }

          // Don't add the reviewer if they've already approved
          if (!approvals.has(reviewer.login)) {
            singles.push(reviewer);
          }
        }
      });

      // Separate reviewers who are not part of requested review groups
      singles.forEach((reviewer) => {
        if (!groupMembers.has(reviewer.login)) {
          if (!prsByReviewer[reviewer.login]) {
            prsByReviewer[reviewer.login] = {
              single: [pr],
              group: []
            };
          } else {
            prsByReviewer[reviewer.login].single.push(pr); 
          }
        }
      })
    }
  });

  return prsByReviewer;
}

const reviewDecisions = {
  reviewRequired: 'REVIEW_REQUIRED',
  changesRequested: 'CHANGES_REQUESTED'
};

const labelsThatBlockPR = new Set([
  'blocked',
  'changes required'
]);

const mergeStatus = {
  mergeable: 'MERGEABLE',
  conflicts: 'CONFLICTING',

}

function prWaitingOnReviewer(pr) {
  if (pr.isDraft) {
    return false;
  }

  // PR not waiting on submitter to implement requested changes
  if (pr.reviewDecision !== reviewDecisions.reviewRequired) {
    return false;
  }

  // PR can be merged without conflicts
  if (pr.mergeable !== mergeStatus.mergeable) {
    return false;
  }

  // PR doesn't contain `blocked` or `changes required` label
  if (pr.labels.some((label) => labelsThatBlockPR.has(label))) {
    return false;
  }

  return true;
}

module.exports = { getPRQuery, groupPRs };
