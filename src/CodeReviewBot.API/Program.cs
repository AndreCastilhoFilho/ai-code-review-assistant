using CodeReviewBot.API;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<GitHubService>();
builder.Services.AddSingleton<AIAnalysisService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/analyze/{prUrl}", async (string prUrl, GitHubService gitHubService, AIAnalysisService aiService) =>
{
    try
    {
        var (owner, repo, prNumber) = ParseGitHubPrUrl(prUrl);
        var prData = await gitHubService.FetchPRDetails(owner, repo, prNumber);
        var analysisResult = await aiService.AnalyzeCode(prData);
        return Results.Ok(analysisResult);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

(string owner, string repo, int prNumber) ParseGitHubPrUrl(string prUrl)
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

app.Run();

