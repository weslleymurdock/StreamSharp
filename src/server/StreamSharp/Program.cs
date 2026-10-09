using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using StreamSharp.Middlewares;
using StreamSharp.Services.Abstract;
using StreamSharp.Services.Concrete;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// 1. Configuração do Rate Limiting / Fila de Concorrência
builder.Services.AddRateLimiter(options =>
{
    // Quando a fila estoura, retorna HTTP 429 Too Many Requests no padrão Problem Details
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política de limitação de concorrência com fila de espera
    options.AddConcurrencyLimiter(policyName: "DownloadQueuePolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 2; // Quantos downloads podem rodar simultaneamente
        limiterOptions.QueueLimit = 5;  // Quantas requisições podem aguardar na fila de espera
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWasmCors", policy =>
    {
        policy.WithOrigins("https://weslleymurdock.github.io", "http://localhost:7000", "http://localhost:7001", "https://localhost:7002") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

builder.Services.AddScoped<IVideoDownloaderService, VideoDownloaderService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton<IDownloadJobManager, DownloadJobManager>();
var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{v1}.json");
    app.MapScalarApiReference("/streamsharp", options =>
    {
        options.WithTitle("Stream Sharp Video Downloader")
            .WithClassicLayout()
            .ForceDarkMode()
            .HideSearch()
            .ShowOperationId()
            .ExpandAllTags()
            .SortTagsAlphabetically()
            .SortOperationsByMethod()
            .PreserveSchemaPropertyOrder();
    });
}
if (!app.Environment.IsDevelopment() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
{
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseCors("BlazorWasmCors");


app.UseAuthorization();

app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                error = entry.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHub<StreamSharp.Hubs.DownloadHub>("/hubs/download");
app.MapControllers();

app.Run();
