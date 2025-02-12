namespace CodeReviewBot.UI
{
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;

    namespace AIReview.UI.Services
    {
        public class PRAnalysisService
        {
            private readonly HttpClient _httpClient;

            public PRAnalysisService(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public async Task<string> AnalyzePR(string repo, int prNumber)
            {
                return await _httpClient.GetFromJsonAsync<string>($"/analyze/{repo}/{prNumber}");
            }
        }
    }
}
