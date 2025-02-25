using CodeReviewBot.API.Interfaces;
using CodeReviewBot.API.Models;
using CodeReviewBot.API.Services;
using CodeReviewBot.API.Shared;
using Octokit;
using System.Text;

namespace CodeReviewBot.API.Strategies
{
    public abstract class BaseCodeAnalysisStrategy : ICodeAnalysisStrategy
    {
        protected readonly GitHubService _githubService;

        protected BaseCodeAnalysisStrategy(GitHubService githubService)
        {
            _githubService = githubService;
        }

        public abstract AiModelType ModelType { get; }

        protected abstract Task<List<CodeReviewComment>> AnalyzeCodeWithPrompt(string codeSnippet, string currentFileName);

        public async Task AnalyzeAndReviewPR(string owner, string repo, int prNumber)
        {
            var pr = await _githubService.GetPullRequest(owner, repo, prNumber);
            var prFiles = await _githubService.GetPullRequestFiles(owner, repo, prNumber);
            var review = await _githubService.CreateReview(owner, repo, prNumber, pr.Head.Sha);

            foreach (var file in prFiles)
            {
                if (!IsFileModified(file)) continue;

                var fileName = Path.GetFileName(file.FileName);
                var diffInfo = ParseDiffInfo(file.Patch);
                var analysisResults = await AnalyzeCodeWithPrompt(file.Patch, fileName);

                await CreateCommentsForFile(file, diffInfo, analysisResults, pr, owner, repo, prNumber, review.Id);
            }
        }

        private static bool IsFileModified(PullRequestFile file)
            => file.Status is "modified" or "added";

        private async Task CreateCommentsForFile(
            PullRequestFile file, 
            DiffInfo diffInfo, 
            List<CodeReviewComment> analysisResults,
            PullRequest pr,
            string owner,
            string repo,
            int prNumber,
            long reviewId)
        {
            foreach (var chunk in diffInfo.Chunks)
            {
                var addedLines = GetAddedLines(chunk);
                if (!addedLines.Any()) continue;

                var chunkComments = GetCommentsForChunk(analysisResults, file.FileName, addedLines);
                if (!chunkComments.Any()) continue;

                await CreateCommentForChunk(
                    chunk, 
                    addedLines.First().Index,
                    chunkComments,
                    file,
                    pr,
                    owner,
                    repo,
                    prNumber,
                    reviewId);
            }
        }

        private static List<(int Index, int LineNumber)> GetAddedLines(DiffChunk chunk)
        {
            return chunk.Content.Split('\n')
                .Select((line, index) => new { line, index })
                .Where(x => x.line.StartsWith("+") && !x.line.StartsWith("++"))
                .Select(x => (
                    Index: x.index,
                    LineNumber: chunk.NewStart + x.index - 1
                ))
                .ToList();
        }

        private static List<CodeReviewComment> GetCommentsForChunk(
            List<CodeReviewComment> analysisResults,
            string fileName,
            List<(int Index, int LineNumber)> addedLines)
        {
            return analysisResults
                .Where(c => c.FileName.Equals(Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase) &&
                           addedLines.Any(l => l.LineNumber == c.LineNumber))
                .ToList();
        }

        private async Task CreateCommentForChunk(
            DiffChunk chunk,
            int position,
            List<CodeReviewComment> comments,
            PullRequestFile file,
            PullRequest pr,
            string owner,
            string repo,
            int prNumber,
            long reviewId)
        {
            var formattedComment = FormatGroupedComments(comments);
            
            await _githubService.CreatePRReviewComment(
                owner,
                repo,
                prNumber,
                pr.Head.Sha,
                file.FileName,
                formattedComment,
                chunk.Content,
                position,
                reviewId
            );
        }

        private DiffInfo ParseDiffInfo(string patch)
        {
            var diffInfo = new DiffInfo();
            var lines = patch.Split('\n');
            var currentChunk = new DiffChunk();

            foreach (var line in lines)
            {
                if (line.StartsWith("@@"))
                {
                    AddCurrentChunkIfNotEmpty(diffInfo, currentChunk);
                    currentChunk = CreateNewChunk(line);
                    continue;
                }

                currentChunk.Content += line + "\n";
            }

            AddCurrentChunkIfNotEmpty(diffInfo, currentChunk);
            return diffInfo;
        }

        private static void AddCurrentChunkIfNotEmpty(DiffInfo diffInfo, DiffChunk chunk)
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                diffInfo.Chunks.Add(chunk);
            }
        }

        private static DiffChunk CreateNewChunk(string line)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"@@ -\d+,?\d* \+(\d+),?\d* @@");
            return new DiffChunk
            {
                NewStart = match.Success ? int.Parse(match.Groups[1].Value) : 0,
                Content = line + "\n"
            };
        }

        protected string FormatGroupedComments(IEnumerable<CodeReviewComment> comments)
        {
            var sb = new StringBuilder();
            var commentsByCategory = comments.GroupBy(c => c.Category)
                .OrderBy(g => g.Key);

            foreach (var categoryGroup in commentsByCategory)
            {
                sb.AppendLine($"📝 **{categoryGroup.Key}**");
                
                var orderedComments = categoryGroup
                    .OrderBy(c => GetSeverityOrder(c.Severity));
                
                foreach (var comment in orderedComments)
                {
                    sb.AppendLine($"{GetSeverityEmoji(comment.Severity)} **{comment.Severity}**: {comment.Message}");
                }
                
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static int GetSeverityOrder(string severity) => severity switch
        {
            "HIGH" => 0,
            "MEDIUM" => 1,
            "LOW" => 2,
            _ => 3
        };

        protected static string GetSeverityEmoji(string severity) => severity switch
        {
            "HIGH" => "🔴",
            "MEDIUM" => "🟡",
            "LOW" => "🟢",
            _ => "ℹ️"
        };

        protected List<CodeReviewComment> ParseAIResponse(string aiResponse)
        {
            var comments = aiResponse.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseCommentLine)
                .Where(c => c != null)
                .ToList();

            return RemoveDuplicateComments(comments);
        }

        private static CodeReviewComment ParseCommentLine(string line)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) return null;

            var parts = trimmedLine.Split(new[] { ' ', ':', '-' }, 5);
            if (parts.Length < 5) return null;

            if (!int.TryParse(parts[1], out int lineNumber) || lineNumber <= 0) return null;

            return new CodeReviewComment
            {
                FileName = parts[0].Trim('[', ']'),
                LineNumber = lineNumber,
                Category = parts[2].Trim(),
                Severity = parts[3].Trim(),
                Message = parts[4].Trim()
            };
        }

        private static List<CodeReviewComment> RemoveDuplicateComments(List<CodeReviewComment> comments)
        {
            return comments
                .GroupBy(c => new { c.FileName, c.LineNumber, c.Category, c.Message })
                .Select(g => g.First())
                .ToList();
        }
    }
}