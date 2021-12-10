using GameServer.Core.Daemon;
using GameServer.Core.Database;
using GameServer.Core.Logger;
using GameServer.Core.Settings;
using GameServer.Data;
using GameServer.Host.Api.Services;
using GameServer.Logger;
using GameServer.Worker;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddSingleton<IGameServerSettings>((s) => GameServerSettings.FromFile(@".\config.yml"));
builder.Services.AddSingleton<IDaemonDataProvider, MongoDBProvider>();
builder.Services.AddSingleton<ILoggerDataProvider, MongoDBProvider>();
builder.Services.AddSingleton<IDaemonWorker, DockerWorker>();
builder.Services.AddSingleton<IPerformanceLogger, PerformanceLogger>();
builder.Services.AddGrpc();
var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ServerService>();
app.MapGrpcService<LoggerService>();
app.MapGrpcService<HealthCheckService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
