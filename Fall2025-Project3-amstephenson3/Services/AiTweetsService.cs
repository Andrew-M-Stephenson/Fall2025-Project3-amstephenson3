using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Fall2025_Project3_amstephenson3.Services
{
    public class AiTweetsService
    {
        private readonly HttpClient _http;
        private readonly string _endpoint; 
        private readonly string _deployment;  
        private readonly string _apiKey;      
        private readonly string _apiVersion;  

        public AiTweetsService(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Missing AzureOpenAI:Endpoint");
            _deployment = config["AzureOpenAI:Deployment"] ?? throw new InvalidOperationException("Missing AzureOpenAI:Deployment");
            _apiKey = config["AzureOpenAI:Key"] ?? throw new InvalidOperationException("Missing AzureOpenAI:Key");
            _apiVersion = config["AzureOpenAI:ApiVersion"] ?? "2024-12-01-preview";

            _http = httpFactory.CreateClient(nameof(AiTweetsService));
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Add("api-key", _apiKey);
        }

        //call
        public Task<List<string>> GenerateTweetsAsync(string actorName)
            => GenerateTweetsAsync(actorName, null);

        //guide model with movies
        public async Task<List<string>> GenerateTweetsAsync(string actorName, IEnumerable<string>? notableMovies)
        {
            var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";

            string moviesLine = (notableMovies != null && notableMovies.Any())
                ? $"Some notable movies: {string.Join(", ", notableMovies.Take(8))}."
                : "Some notable movies: (not provided).";

            var payload = new
            {
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant. Return ONLY valid JSON; no extra text." },
                    new { role = "user", content = $@"
                        Generate twenty short, varied, natural-language tweets about the actor '{actorName}'.
                        {moviesLine}
                        No hashtags or @mentions. Mix praise, neutral chatter, and light criticism. Keep each to 1–2 sentences.
                        Return ONLY JSON with this exact shape:
                        {{ ""tweets"": [""..."",""..."" ] }}
                        Ensure there are exactly 20 strings in ""tweets"" and no extra properties."}
                },
                temperature = 0.7,
                max_tokens = 800,
                response_format = new { type = "json_object" }, //json
                model = _deployment
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Azure OpenAI error {resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var innerText = doc.RootElement
                               .GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString() ?? "";

            try
            {
                using var inner = JsonDocument.Parse(innerText);
                var tweets = inner.RootElement.GetProperty("tweets")
                    .EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(20)
                    .ToList();

                return tweets;
            }
            catch
            {
                return FallbackExtractLines(innerText, 20);
            }
        }

        private static List<string> FallbackExtractLines(string text, int max)
        {
            var lines = new List<string>();
            foreach (var line in text.Split('\n'))
            {
                var s = line.Trim().TrimStart('-', '*').Trim();
                if (!string.IsNullOrWhiteSpace(s)) lines.Add(s);
                if (lines.Count >= max) break;
            }
            return lines;
        }
    }
}
