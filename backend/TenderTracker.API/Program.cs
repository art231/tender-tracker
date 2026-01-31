using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TenderTracker.API.BackgroundServices;
using TenderTracker.API.Clients;
using TenderTracker.API.Config;
using TenderTracker.API.Data;
using TenderTracker.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TenderTracker API",
        Version = "v1",
        Description = "API для отслеживания тендеров с GosPlan"
    });
});

// Configure CORS for AngularJS frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:8080", "http://localhost:3000", 
                               "http://localhost:4200", "http://localhost:4300")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Configure Database with retry logic
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => 
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }));

// Configure HttpClient for GosPlan API
builder.Services.AddHttpClient<IGosPlanApiClient, GosPlanApiClient>();

// Configure GosPlan API options
builder.Services.Configure<GosPlanApiOptions>(builder.Configuration.GetSection("GosPlanApi"));

// Register services
builder.Services.AddScoped<ISearchQueryService, SearchQueryService>();
builder.Services.AddScoped<IFoundTenderService, FoundTenderService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ITechnologyAnalysisService, TechnologyAnalysisService>();
builder.Services.AddScoped<IDocumentExportService, DocumentExportService>();

// Configure export settings
builder.Services.Configure<ExportSettings>(builder.Configuration.GetSection("ExportSettings"));

// Configure technology stack
builder.Services.Configure<TechnologyStackConfig>(options =>
{
    var defaultConfig = DefaultTechnologyStack.GetDefault();
    options.Technologies = defaultConfig.Technologies;
    options.Settings = defaultConfig.Settings;
});

// Add health checks (temporarily disabled for testing)
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"));

// Register background services
builder.Services.AddHostedService<TenderSearchBackgroundService>();
builder.Services.AddHostedService<TenderCleanupBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // В development среде отключаем HTTPS редирект для удобства тестирования
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TenderTracker API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration(); // Показывать время выполнения запроса
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TenderTracker API v1");
        options.RoutePrefix = "swagger";
    });
}

// Global error handling middleware
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\":\"An unexpected error occurred. Please try again later.\"}");
    });
});

app.UseRouting();

app.UseCors("AllowAngularApp");

// В development среде не используем HTTPS редирект
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();

// Health check endpoint (temporarily disabled)
app.MapHealthChecks("/health");

app.MapControllers();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
