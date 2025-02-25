using FileSystemApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

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

app.UseCors("AllowAllOrigins");

app.MapControllers();

app.Run();
