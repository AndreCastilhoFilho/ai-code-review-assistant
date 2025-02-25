using CodeReviewBot.API.Interfaces;
using CodeReviewBot.API.Services;
using CodeReviewBot.API.Shared;
using CodeReviewBot.API.Strategies;
using CodeReviewBot.API.Utils;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AI Code Review API", Version = "v1" });

    c.EnableAnnotations();

});

builder.Services.AddSingleton<GitHubService>();
builder.Services.AddSingleton<ICodeAnalysisStrategy, HuggingFaceCodeAnalysisStrategy>();
builder.Services.AddSingleton<ICodeAnalysisStrategy, OpenAiCodeAnalysisStrategy>();

builder.Services.AddSingleton<CodeAnalysisStrategyFactory>();


builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/analyze/{prUrl}", async (string prUrl, CodeAnalysisStrategyFactory strategyFactory, GitHubService gitHubService, AiModelType? modelType) =>
{
    try
    {
        var (owner, repo, prNumber) = StringFormatHelper.ParseGitHubPrUrl(prUrl);
        var selectedModel = modelType ?? AiModelType.HuggingFace;
        var strategy = strategyFactory.GetStrategy(selectedModel);

        await strategy.AnalyzeAndReviewPR(owner, repo, prNumber);
        return Results.Ok("PR analysed sucessfully. Comments were done in PR");
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});



app.Run();

