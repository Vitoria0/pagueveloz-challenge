using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PagueVeloz.TransactionProcessor.Application;
using PagueVeloz.TransactionProcessor.Infrastructure;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        new JsonFormatter(),
        path: "logs/pagueveloz-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PagueVeloz Transaction Processor API",
        Version = "v1",
        Description = "API para processamento de transações financeiras",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "PagueVeloz",
            Email = "support@pagueveloz.com"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Adicionar camadas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(10);
    setup.MaximumHistoryEntriesPerEndpoint(50);
}).AddInMemoryStorage();

// Métricas Prometheus - já configurado pelo pacote prometheus-net.AspNetCore

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PagueVeloz Transaction Processor API v1");
        c.RoutePrefix = "swagger"; // Swagger UI estará em /swagger
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
});

// Métricas Prometheus
app.UseMetricServer();
app.UseHttpMetrics();

// Aplicar migrações (opcional - não bloqueia a inicialização)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.CanConnect())
        {
            context.Database.Migrate();
            Log.Information("Migrações aplicadas com sucesso");
        }
        else
        {
            Log.Warning("Não foi possível conectar ao banco de dados. Migrações não foram aplicadas.");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Erro ao aplicar migrações. A aplicação continuará sem aplicar migrações. Execute manualmente: dotnet ef database update");
    }
}

try
{
    Log.Information("Iniciando PagueVeloz Transaction Processor API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

