using System.Security.Principal;
using Mtls.Worker.Configuration;

Console.WriteLine("[Worker] starting mTLS worker...");
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddNamedHttpClient();
builder.Services.AddHostedService<mtls.worker.Services.BackgroundTask>();

var app = builder.Build();
app.Run();

