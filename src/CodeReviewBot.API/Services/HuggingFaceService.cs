using System.Text.Json;
using System.Text;

namespace CodeReviewBot.API.Services
{
    public class HuggingFaceService

    {
        private readonly HttpClient _httpClient;
        private readonly string _huggingFaceApiKey;
        private const string HuggingFaceUrl = "https://api-inference.huggingface.co/models/mistralai/Mistral-7B-Instruct-v0.3";

        public HuggingFaceService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _huggingFaceApiKey = configuration["LlamaAI:ApiKey"];
        }

        public async Task<string> AnalyzeCode(string codeSnippet)
        {
            var prompt = $"{Prompt.System}  Code snippet:\n{codeSnippet}";

            var requestBody = new
            {
                inputs = prompt,
                parameters = new
                {
                    max_new_tokens = 1024,
                    temperature = 0.3,
                    top_p = 0.9
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _huggingFaceApiKey);

            var response = await _httpClient.PostAsync(HuggingFaceUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            responseString = responseString.Trim().Trim('"'); 

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse[0].TryGetProperty("generated_text", out var generatedText))
            {
                return FormatResponse(generatedText.GetString().Trim(), prompt);
            }

            throw new Exception("Unexpected response format from Hugging Face API.");

        }

        private string FormatResponse(string response, string prompt)
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

    }
}

