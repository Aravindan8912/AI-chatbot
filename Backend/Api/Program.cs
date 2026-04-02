using Api;
using Application.Interfaces;
using Application.Services;
using DotNetEnv;
using Infrastructure.OpenAI;
using Infrastructure.Web;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine("logs", "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

// Load Backend/.env before configuration (OPENAI_API_KEY, etc.)
IReadOnlyList<KeyValuePair<string, string>>? dotEnv = null;
foreach (var path in EnvFileLocator.ResolveEnvFilePaths())
{
    if (!File.Exists(path))
        continue;
    var opts = new LoadOptions(setEnvVars: true, clobberExistingVars: true);
    dotEnv = Env.Load(path, opts).ToList();
    break;
}

try
{
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine("logs", "api-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14));

// Bridge .env into IConfiguration (some hosts/platforms do not surface DotNetEnv into GetEnvironmentVariable reliably).
if (dotEnv is { Count: > 0 })
{
    var extra = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var kv in dotEnv)
    {
        if (string.IsNullOrWhiteSpace(kv.Key))
            continue;
        extra[kv.Key] = kv.Value;
        if (kv.Key.Equals("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
            extra["OpenAI:ApiKey"] = kv.Value;
        if (kv.Key.Equals("OPENAI__Model", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kv.Value))
            extra["OpenAI:Model"] = kv.Value;
    }

    builder.Configuration.AddInMemoryCollection(extra);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddHttpClient<IOpenAIClient, OpenAIClient>();
builder.Services.AddHttpClient<IWebsiteAnalyzer, WebsiteAnalyzer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
