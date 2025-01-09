using Aspire.Hosting.ApplicationModel;
using MartinCostello.SqlLocalDb;
using Microsoft.Extensions.Logging;

namespace LocalDbSupport;

internal class LocalDbService(ISqlLocalDbApi api, ResourceLoggerService loggerService, ResourceNotificationService notifierService)
{
    public ISqlLocalDbInstanceInfo GetInstance(string name) => api.GetOrCreateInstance(name);

    public ISqlLocalDbInstanceInfo GetTemporaryInstance(bool deleteFiles) => api.CreateTemporaryInstance(deleteFiles).GetInstanceInfo();

    public async Task CreateInstance(LocalDbInstanceResource resource)
    {
        var logger = loggerService.GetLogger(resource);
        
        logger.LogInformation("Creating LocalDb instance {name}", resource.Name);
        await notifierService.PublishUpdateAsync(resource, state => state with { State = KnownResourceStates.Starting });

        var instance = GetInstance(resource.Name);
        instance.Manage().Start();

        await notifierService.PublishUpdateAsync(resource, state => state with { State = KnownResourceStates.Running, StartTimeStamp = DateTime.Now });
    }
}
