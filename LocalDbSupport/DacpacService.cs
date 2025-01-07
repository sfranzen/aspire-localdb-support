using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;

namespace LocalDbSupport;

internal class DacpacService(ResourceLoggerService loggerService, ResourceNotificationService notifierService)
{
    public async Task Deploy(string dacpacPath, LocalDbDatabaseResource target, CancellationToken token)
    {
        var logger = loggerService.GetLogger(target);

        try
        {
            logger.LogInformation("Deploying dacpac from {path} to {database}", dacpacPath, target.DatabaseName);
            await notifierService.PublishUpdateAsync(target, state => state with { State = KnownResourceStates.Starting });

            var connectionString = await target.GetConnectionStringAsync(token);
            var dacService = new DacServices(connectionString);
            var dac = DacPackage.Load(dacpacPath);
            dacService.Deploy(dac, target.DatabaseName, upgradeExisting: true, cancellationToken: token);
            await notifierService.PublishUpdateAsync(target, state => state with { State = KnownResourceStates.Running, StartTimeStamp = DateTime.Now });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deploy dacpac.");
            await notifierService.PublishUpdateAsync(target, state => state with { State = KnownResourceStates.FailedToStart });
        }
    }
}
