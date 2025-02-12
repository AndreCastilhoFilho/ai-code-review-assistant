using Octokit;
using System.Text;


namespace CodeReviewBot.API.Services
{
    public class GitHubService
    {
        private readonly GitHubClient _client;

        public GitHubService(IConfiguration configuration)
        {
            _client = new GitHubClient(new ProductHeaderValue("AIReviewBot"))
            {
                Credentials = new Credentials(configuration["Github:Token"])
            };
        }

        public async Task<string> FetchPRDetails(string owner, string repo, int prNumber)
        {
            var pr = await _client.PullRequest.Get(owner, repo, prNumber);
            // Fetch the files changed in the pull request
            var prFiles = await _client.PullRequest.Files(owner, repo, prNumber);

            // StringBuilder to accumulate the diffs
            var diffBuilder = new StringBuilder();

            foreach (var file in prFiles)
            {
                // Append the filename and its diff (patch)
                diffBuilder.AppendLine($"File: {file.FileName}");
                diffBuilder.AppendLine(file.Patch);
                diffBuilder.AppendLine();
            }

            return diffBuilder.ToString();
        }
    }
}
