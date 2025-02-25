using FileSystemApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<DbProvider>();
builder.Services.AddControllers();

var apiListenPortStr = Environment.GetEnvironmentVariable("API_LISTEN_PORT");
if (apiListenPortStr is not null && int.TryParse(apiListenPortStr, out var apiListenPort))
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(apiListenPort);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapControllers();

app.Run();
