using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using SapImpactAnalyzer.Services;
using SapImpactAnalyzer.Services.AI;
using SapImpactAnalyzer.Services.Search;
using SapImpactAnalyzer.Services.Transport;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.Configure<AzureOpenAIConfig>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<AzureSearchConfig>(builder.Configuration.GetSection("AzureSearch"));

builder.Services.AddSingleton<AzureOpenAIClient>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IOptions<AzureOpenAIConfig>>().Value;
    return new AzureOpenAIClient(
        new Uri(config.Endpoint),
        new AzureKeyCredential(config.ApiKey));
});

builder.Services.AddSingleton<SearchIndexClient>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IOptions<AzureSearchConfig>>().Value;
    return new SearchIndexClient(
        new Uri(config.Endpoint),
        new AzureKeyCredential(config.ApiKey));
});

builder.Services.AddSingleton<SearchClient>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IOptions<AzureSearchConfig>>().Value;
    return new SearchClient(
        new Uri(config.Endpoint),
        config.IndexName,
        new AzureKeyCredential(config.ApiKey));
});

//builder.Services.AddSingleton<OpenAIClient>(serviceProvider =>
//{
//    var config = serviceProvider.GetRequiredService<IOptions<AzureOpenAIConfig>>().Value;
//    return new OpenAIClient(
//        new Uri(config.Endpoint),
//        new AzureKeyCredential(config.ApiKey));
//});


// register services
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISAPService, SAPService>();
builder.Services.AddScoped<ITransportAnalysisService, TransportAnalysisService>();
builder.Services.AddScoped<IDependencyAnalysisService, DependencyAnalysisService>();
builder.Services.AddScoped<AzureOpenAIService>();
// If you want to use AzureOpenAIService as the default, swap the above line to use AzureOpenAIService instead.
// Remove all references to the old interfaces.
builder.Services.AddScoped<IUnifiedAIAnalysisService, AzureOpenAIService>();
builder.Services.AddScoped<ISearchIndexManager, AzureSearchIndexManager>();
builder.Services.AddHealthChecks()
    .AddCheck<AzureSearchHealthCheck>("azure-search");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IOptions<AzureOpenAIConfig>>().Value;
    var client = serviceProvider.GetRequiredService<AzureOpenAIClient>();
    var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(
            deploymentName: config.DeploymentName,
            endpoint: config.Endpoint,
            apiKey: config.ApiKey)
        //.AddAzureOpenAITextEmbeddingGeneration(
        //    deploymentName: config.EmbeddingDeploymentName,
        //    endpoint: config.Endpoint,
        //    apiKey: config.ApiKey)
        .Build();
    return kernel;
});
builder.Services.AddScoped<EnhancedTransportAnalysisService>();
builder.Services.AddScoped<SAPAnalysisPlugin>();
builder.Services.AddScoped<IVectorSearchService, AzureSearchVectorService>();
builder.Services.AddScoped<KnowledgeIngestionService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseStaticFiles(); // This serves HTML files
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
