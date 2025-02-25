using CodeReviewBot.API.Interfaces;
using CodeReviewBot.API.Services;
using CodeReviewBot.API.Shared;
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

        private (DiffInfo diffInfo, Dictionary<int, int> lineMapping) ParseDiffWithLineNumbers(string patch)
        {
            var diffInfo = new DiffInfo();
            var lines = patch.Split('\n');
            var currentChunk = new DiffChunk();
            var diffPosition = 0;
            var lineNumber = 0;
            var chunkLines = new List<string>();
            var absoluteLineMapping = new Dictionary<int, int>();

            foreach (var line in lines)
            {
                if (line.StartsWith("@@"))
                {
                    if (chunkLines.Count > 0)
                    {
                        currentChunk.Content = string.Join("\n", chunkLines);
                        diffInfo.Chunks.Add(currentChunk);
                    }

                    chunkLines = new List<string> { line };
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"@@ -\d+,?\d* \+(\d+),?\d* @@");
                    if (match.Success)
                    {
                        currentChunk = new DiffChunk
                        {
                            NewStart = int.Parse(match.Groups[1].Value),
                            LineMapping = new Dictionary<int, int>()
                        };
                        lineNumber = currentChunk.NewStart - 1;
                    }
                    diffPosition = 0;
                    continue;
                }

                chunkLines.Add(line);
                if (line.StartsWith("+"))
                {
                    lineNumber++;
                    diffPosition++;
                    absoluteLineMapping[lineNumber] = diffPosition;
                    currentChunk.LineMapping[lineNumber] = diffPosition;
                }
                else if (!line.StartsWith("-"))
                {
                    lineNumber++;
                    diffPosition++;
                }
                else
                {
                    diffPosition++;
                }
            }

            if (chunkLines.Count > 0)
            {
                currentChunk.Content = string.Join("\n", chunkLines);
                diffInfo.Chunks.Add(currentChunk);
            }

            return (diffInfo, absoluteLineMapping);
        }

        public async Task AnalyzeAndReviewPR(string owner, string repo, int prNumber)
        {
            var pr = await _githubService.GetPullRequest(owner, repo, prNumber);
            var prFiles = await _githubService.GetPullRequestFiles(owner, repo, prNumber);
            var review = await _githubService.CreateReview(owner, repo, prNumber, pr.Head.Sha);

            foreach (var file in prFiles)
            {
                if (file.Status != "modified" && file.Status != "added")
                    continue;

                var simpleFileName = Path.GetFileName(file.FileName);
                var diffInfo = ParseDiffInfo(file.Patch);
                var analysisResults = await AnalyzeCodeWithPrompt(file.Patch, simpleFileName);

                // Processar cada chunk separadamente
                foreach (var chunk in diffInfo.Chunks)
                {
                    var chunkLines = chunk.Content.Split('\n');
                    var addedLines = chunkLines
                        .Select((line, index) => new { line, index })
                        .Where(x => x.line.StartsWith("+") && !x.line.StartsWith("++"))
                        .Select(x => new 
                        { 
                            Index = x.index,
                            LineNumber = chunk.NewStart + x.index - 1 // Ajustar para o número real da linha
                        })
                        .ToList();

                    if (!addedLines.Any()) continue;

                    // Filtrar comentários que são específicos para as linhas adicionadas neste chunk
                    var chunkComments = analysisResults
                        .Where(c => c.FileName.Equals(simpleFileName, StringComparison.OrdinalIgnoreCase) &&
                                   addedLines.Any(l => l.LineNumber == c.LineNumber))
                        .ToList();

                    if (!chunkComments.Any()) continue;

                    // Usar o índice da primeira linha adicionada como posição do comentário
                    var position = addedLines.First().Index;
                    var formattedComment = FormatGroupedComments(chunkComments);
                    
                    await _githubService.CreatePRReviewComment(
                        owner,
                        repo,
                        prNumber,
                        pr.Head.Sha,
                        file.FileName,
                        formattedComment,
                        chunk.Content,
                        position,
                        review.Id
                    );
                }
            }
        }

        protected string FormatGroupedComments(IEnumerable<CodeReviewComment> comments)
        {
            var sb = new StringBuilder();

            var commentsByCategory = comments.GroupBy(c => c.Category)
                .OrderBy(g => g.Key);

            foreach (var categoryGroup in commentsByCategory)
            {
                sb.AppendLine($"📝 **{categoryGroup.Key}**");
                
                var orderedComments = categoryGroup.OrderBy(c => c.Severity == "HIGH" ? 0 : 
                                                       c.Severity == "MEDIUM" ? 1 : 2);
                
                foreach (var comment in orderedComments)
                {
                    sb.AppendLine($"{GetSeverityEmoji(comment.Severity)} **{comment.Severity}**: {comment.Message}");
                }
                
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        protected string GetSeverityEmoji(string severity)
        {
            return severity switch
            {
                "HIGH" => "🔴",
                "MEDIUM" => "🟡",
                "LOW" => "🟢",
                _ => "ℹ️"
            };
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
                    if (!string.IsNullOrEmpty(currentChunk.Content))
                    {
                        diffInfo.Chunks.Add(currentChunk);
                    }

                    var match = System.Text.RegularExpressions.Regex.Match(line, @"@@ -\d+,?\d* \+(\d+),?\d* @@");
                    if (match.Success)
                    {
                        currentChunk = new DiffChunk
                        {
                            NewStart = int.Parse(match.Groups[1].Value),
                            Content = line + "\n"
                        };
                    }
                    continue;
                }

                currentChunk.Content += line + "\n";
            }

            if (!string.IsNullOrEmpty(currentChunk.Content))
            {
                diffInfo.Chunks.Add(currentChunk);
            }

            return diffInfo;
        }

        private (int position, string diffHunk) GetPositionAndDiffHunk(DiffInfo diffInfo, int targetLine)
        {
            foreach (var chunk in diffInfo.Chunks)
            {
                if (chunk.LineMapping.ContainsKey(targetLine))
                {
                    var position = chunk.LineMapping[targetLine];
                    var lines = chunk.Content.Split('\n');
                    var contextLines = new List<string> { lines[0] };

                    for (var i = Math.Max(1, position - 3); i <= Math.Min(lines.Length - 1, position + 3); i++)
                    {
                        contextLines.Add(lines[i]);
                    }

                    return (position, string.Join("\n", contextLines));
                }
            }

            var nearestLine = diffInfo.Chunks
                .SelectMany(c => c.LineMapping)
                .OrderBy(kv => Math.Abs(kv.Key - targetLine))
                .FirstOrDefault();

            if (nearestLine.Key != 0)
            {
                var chunk = diffInfo.Chunks.First(c => c.LineMapping.ContainsKey(nearestLine.Key));
                var lines = chunk.Content.Split('\n');
                var contextLines = new List<string> { lines[0] };

                var position = nearestLine.Value;
                for (var i = Math.Max(1, position - 3); i <= Math.Min(lines.Length - 1, position + 3); i++)
                {
                    contextLines.Add(lines[i]);
                }

                return (nearestLine.Value, string.Join("\n", contextLines));
            }

            return (-1, null);
        }

        protected List<CodeReviewComment> ParseAIResponse(string aiResponse)
        {
            var comments = new List<CodeReviewComment>();
            
            var lines = aiResponse.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;
                
                var fileLineParts = trimmedLine.Split(' ', 2);
                if (fileLineParts.Length != 2) continue;

                var fileAndLine = fileLineParts[0].Split(':');
                if (fileAndLine.Length != 2) continue;

                var categoryParts = fileLineParts[1].Split(new[] { ':', '-' }, 3);
                if (categoryParts.Length != 3) continue;

                if (int.TryParse(fileAndLine[1], out int lineNumber))
                {
                    // Verificar se é uma linha que foi realmente modificada
                    if (lineNumber > 0)
                    {
                        comments.Add(new CodeReviewComment
                        {
                            FileName = fileAndLine[0].Trim('[', ']'), // Remover os colchetes do nome do arquivo
                            LineNumber = lineNumber,
                            Category = categoryParts[0].Trim(),
                            Severity = categoryParts[1].Trim(),
                            Message = categoryParts[2].Trim()
                        });
                    }
                }
            }
            
            // Remover comentários duplicados
            return comments
                .GroupBy(c => new { c.FileName, c.LineNumber, c.Category, c.Message })
                .Select(g => g.First())
                .ToList();
        }
    }

    public class CodeReviewComment
    {
        public string FileName { get; set; }
        public int LineNumber { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
    }

    public class DiffInfo
    {
        public List<DiffChunk> Chunks { get; set; } = new();
    }

    public class DiffChunk
    {
        public int NewStart { get; set; }
        public string Content { get; set; }
        public Dictionary<int, int> LineMapping { get; set; } = new();
    }
}