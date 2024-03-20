using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MZ.Law.App;
using Serilog;
using Serilog.Sinks.MSSqlServer;

var configuration = GetConfiguration();

Log.Logger = CreateSerilogLogger(configuration);
try
{
    Log.Information("Configuring web host ({ApplicationContext})...", Program.AppName);
    var host = BuildWebHost(configuration, args);


    Log.Information("Starting web host ({ApplicationContext})...", Program.AppName);
    host.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", Program.AppName);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

IConfiguration GetConfiguration()
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

#if DEBUG
    builder.AddJsonFile("appsettings.development.json");
#endif

    return builder.Build();
}

(int httpPort, int grpcPort) GetDefinedPorts(IConfiguration config)
{
    var grpcPort = config.GetValue("GRPC_PORT", 8000);
    var port = config.GetValue("PORT", 82);
    return (port, grpcPort);
}
Serilog.ILogger CreateSerilogLogger(IConfiguration config)
{
    var connectionString = configuration.GetSection("ConnectionStrings").GetValue<string>("RegistrationDb");

    ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));
    return new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .Enrich.WithProperty("ApplicationContext", Program.AppName)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .Enrich.With(new MZEnricher(configuration["Logging:Application"], configuration["Logging:Module"]))
        .ReadFrom.Configuration(config)
        .WriteTo.MSSqlServer(
            connectionString: connectionString,
            sinkOptions: new MSSqlServerSinkOptions
            {

                SchemaName = "dbo",
                AutoCreateSqlTable = true,
                TableName = "LawApp_Logs",
            }
        )
        .CreateLogger();
}
IHost BuildWebHost(IConfiguration config, string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webHost =>
        {
            webHost.ConfigureKestrel(options =>
                {
                    var ports = GetDefinedPorts(config);
                    options.Listen(IPAddress.Any, ports.httpPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    options.Listen(IPAddress.Any, ports.grpcPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });

                })
                .ConfigureAppConfiguration(x => x.AddConfiguration(config))
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory());
        })
        .UseSerilog()
        .Build();

public partial class Program
{
    public const string AppName = "MZ.LawApp.API";
}/*

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
*/
