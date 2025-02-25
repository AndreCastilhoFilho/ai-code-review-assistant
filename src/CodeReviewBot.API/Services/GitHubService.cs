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

        public async Task CreatePRComment(string owner, string repo, int prNumber, string comment)
        {
            try
            {
                await _client.Issue.Comment.Create(owner, repo, prNumber, comment);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao criar comentário no PR: {ex.Message}", ex);
            }
        }

        public async Task<PullRequest> GetPullRequest(string owner, string repo, int prNumber)
        {
            return await _client.PullRequest.Get(owner, repo, prNumber);
        }

        public async Task<IReadOnlyList<PullRequestFile>> GetPullRequestFiles(string owner, string repo, int prNumber)
        {
            return await _client.PullRequest.Files(owner, repo, prNumber);
        }

        public async Task<PullRequestReview> CreateReview(string owner, string repo, int prNumber, string commitId)
        {
            var review = new PullRequestReviewCreate
            {
                CommitId = commitId,
                Body = "Automated code review",
                Event = PullRequestReviewEvent.Comment
            };

            return await _client.PullRequest.Review.Create(owner, repo, prNumber, review);
        }

        public async Task CreatePRReviewComment(string owner, string repo, int prNumber, 
            string commitId, string path, string body, string diffHunk, int position, long? reviewId = null)
        {
            try
            {
                var comment = new PullRequestReviewCommentCreate(body, commitId, path, position);
                await _client.PullRequest.ReviewComment.Create(owner, repo, prNumber, comment);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao criar comentário de revisão no PR: {ex.Message}", ex);
            }
        }
    }
}
