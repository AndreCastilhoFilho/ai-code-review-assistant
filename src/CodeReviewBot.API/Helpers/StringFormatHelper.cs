using System.Text;

namespace CodeReviewBot.API.Utils
{
    internal static class StringFormatHelper
    {
        public static string Format(string response, string prompt)
        {
            // Remove the prompt from the response
            response = response.Replace(prompt, "").Trim();

            // Fix escaped newlines and quote characters
            response = response.Replace("\\r\\n", "<br>").Replace("\\\"", "\"");

            // Decode Unicode characters
            response = System.Net.WebUtility.HtmlDecode(response);

            // Convert Markdown syntax to HTML
            response = response.Replace("**", "<strong>").Replace("```", "<pre>").Replace("\n", "<br>");

            var formattedResponse = new StringBuilder();
            formattedResponse.AppendLine("<h2>AI Code Review Report</h2>");
            formattedResponse.AppendLine($"<p>{response}</p>");

            return formattedResponse.ToString();
        }

        internal static (string owner, string repo, int prNumber) ParseGitHubPrUrl(string prUrl)
        {
            var decodedUrl = Uri.UnescapeDataString(prUrl);
            // Example URL: https://github.com/owner/repo/pull/123
            var uri = new Uri(decodedUrl);
            var segments = uri.Segments;
            if (segments.Length < 5 || segments[3].TrimEnd('/') != "pull")
            {
                throw new ArgumentException("Invalid GitHub PR URL format.");
            }

            var owner = segments[1].TrimEnd('/');
            var repo = segments[2].TrimEnd('/');
            if (!int.TryParse(segments[4].TrimEnd('/'), out int prNumber))
            {
                throw new ArgumentException("Invalid PR number in URL.");
            }

            return (owner, repo, prNumber);
        }
    }

}
