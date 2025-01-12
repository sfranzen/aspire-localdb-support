using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;

namespace Aspire.Hosting;

internal class DacpacService(ResourceLoggerService loggerService, ResourceNotificationService notifierService)
{
    public async Task Deploy(string dacpacPath, SqlLocalDbDatabaseResource target, CancellationToken token)
    {
        var logger = loggerService.GetLogger(target);
        void DacService_Message(object? sender, DacMessageEventArgs e) => logger.LogInformation("{message}", e.Message.ToString());

        try
        {
            await notifierService.PublishUpdateAsync(target, state => state with { State = KnownResourceStates.Starting });
            logger.LogInformation("Deploying dacpac from {path} to {database}", dacpacPath, target.DatabaseName);

            var connectionString = await target.GetConnectionStringAsync(token);
            var dacService = new DacServices(connectionString);

            dacService.Message += DacService_Message;
            var dac = DacPackage.Load(dacpacPath);
            dacService.Deploy(dac, target.DatabaseName, upgradeExisting: true, cancellationToken: token);
            dacService.Message -= DacService_Message;

            await notifierService.PublishUpdateAsync(target, state => state with { State = KnownResourceStates.Running, StartTimeStamp = DateTime.Now });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deploy dacpac.");
            await notifierService.PublishUpdateAsync(target, state => state with { State = KnownResourceStates.FailedToStart });
        }
    }
}
