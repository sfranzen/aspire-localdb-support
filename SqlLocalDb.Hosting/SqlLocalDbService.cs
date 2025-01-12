using Aspire.Hosting.ApplicationModel;
using MartinCostello.SqlLocalDb;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

internal class SqlLocalDbService(ResourceLoggerService loggerService, ResourceNotificationService notifierService)
{
    public class LoggerFactory(ILogger logger) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        { }

        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public SqlLocalDbApi GetApi(SqlLocalDbInstanceResource resource)
    {
        var logger = loggerService.GetLogger(resource);
        var loggerFactory = new LoggerFactory(logger);
        return new SqlLocalDbApi(loggerFactory);
    }

    public async Task<ISqlLocalDbInstanceInfo> GetOrCreateInstance(SqlLocalDbInstanceResource resource)
    {
        await notifierService.PublishUpdateAsync(resource, state => state with { State = KnownResourceStates.Starting });

        using var api = GetApi(resource);
        var instance = api.GetOrCreateInstance(resource.Name);
        instance.Manage().Start();

        await notifierService.PublishUpdateAsync(resource, state => state with { State = KnownResourceStates.Running, StartTimeStamp = DateTime.Now });
        return instance;
    }
}
