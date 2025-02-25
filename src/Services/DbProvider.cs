using StackExchange.Redis;

namespace FileSystemApi.Services;

public class DbProvider
{
    public IDatabase Database { get; }
    public DbProvider(ILogger<DbProvider> logger)
    {
        var connectionStr = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionStr))
        {
            logger.LogError("REDIS_CONNECTION_STRING is not set");
            throw new Exception("REDIS_CONNECTION_STRING is not set");
        }

        ConnectionMultiplexer c = ConnectionMultiplexer.Connect(connectionStr);
        Database = c.GetDatabase();
    }
}
