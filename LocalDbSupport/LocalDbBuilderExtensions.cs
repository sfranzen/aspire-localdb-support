using Aspire.Hosting.ApplicationModel;
using LocalDbSupport;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aspire.Hosting;

public static class LocalDbBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="LocalDbInstanceResource"/> to the application model, representing a named LocalDb instance.
    /// </summary>
    /// <param name="builder">An <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource and LocalDb instance.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the instance resource.</returns>
    public static IResourceBuilder<LocalDbInstanceResource> AddLocalDbInstance(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        var instanceRes = new LocalDbInstanceResource(name);
        var healthCheckKey = $"{name}_check";
        string? connectionString = null;

        builder.Services.TryAddSingleton<LocalDbService>();
        builder.Services
            .AddSqlLocalDB()
            .AddHealthChecks()
            .AddSqlServer(sp => connectionString ?? throw new InvalidOperationException("Connection string is unavailable"), name: healthCheckKey);

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, token) => {
            var dbService = @event.Services.GetRequiredService<LocalDbService>();
            await dbService.CreateInstance(instanceRes);
            connectionString = await instanceRes.GetConnectionStringAsync(token);
        });

        var initialState = new CustomResourceSnapshot
        {
            Properties = [],
            ResourceType = "LocalDbInstance",
            CreationTimeStamp = DateTime.Now
        };

        return builder.AddResource(instanceRes).WithInitialState(initialState).WithHealthCheck(healthCheckKey);
    }

    /// <summary>
    /// Adds a <see cref="LocalDbDatabaseResource"/> to the application model as a child of a <see cref="LocalDbInstanceResource"/>.
    /// </summary>
    /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the LocalDb instance.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="databaseName">An optional name for the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the database resource.</returns>
    public static IResourceBuilder<LocalDbDatabaseResource> AddDatabase(this IResourceBuilder<LocalDbInstanceResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        databaseName ??= name;
        var localDatabase = new LocalDbDatabaseResource(name, databaseName, builder.Resource);
        var initialState = new CustomResourceSnapshot
        {
            Properties = [new("DatabaseName", databaseName)],
            ResourceType = "LocalDbDatabase",
            CreationTimeStamp = DateTime.Now
        };
        builder.Resource.AddDatabase(name, databaseName);
        return builder.ApplicationBuilder.AddResource(localDatabase).WithInitialState(initialState);
    }

    /// <summary>
    /// Publishes the .dacpac file to the LocalDb database <see cref="LocalDbDatabaseResource"/>.
    /// </summary>
    /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the LocalDb database to publish to.</param>
    /// <param name="dacpacPath">The path to the .dacpac file.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the database resource.</returns>
    public static IResourceBuilder<LocalDbDatabaseResource> WithDacpac(this IResourceBuilder<LocalDbDatabaseResource> builder, string dacpacPath)
    {
        var dbResource = builder.Resource;
        Task deploy(IServiceProvider services, CancellationToken token)
        {
            var deployer = services.GetRequiredService<DacpacService>();
            return deployer.Deploy(dacpacPath, dbResource, token);
        }

        builder.ApplicationBuilder.Services.TryAddSingleton<DacpacService>();
        builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(dbResource.Parent, (@event, token) => deploy(@event.Services, token));

        builder.WithCommand("redeploy", "Redeploy", async (context) =>
        {
            await deploy(context.ServiceProvider, context.CancellationToken);
            return new ExecuteCommandResult { Success = true };
        }, updateState: (context) => context.ResourceSnapshot?.State == KnownResourceStates.Running ? ResourceCommandState.Enabled : ResourceCommandState.Disabled,
            displayDescription: "Redeploys the associated dacpac to the target database.",
            iconName: "ArrowReset",
            iconVariant: IconVariant.Filled,
            isHighlighted: true);

        return builder;
    }

    /// <summary>
    /// Publishes the SQL Server Database project to the LocalDb target <see cref="LocalDbDatabaseResource"/>.
    /// </summary>
    /// <param name="builder">An <see cref="IResourceBuilder{T}"/> representing the SQL Server Database project to publish.</param>
    /// <param name="target">An <see cref="IResourceBuilder{T}"/> representing the target <see cref="LocalDbDatabaseResource"/> to publish the SQL Server Database project to.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> that can be used to further customize the project resource.</returns>
    public static IResourceBuilder<SqlProjectResource> WithReference(this IResourceBuilder<SqlProjectResource> builder, IResourceBuilder<LocalDbDatabaseResource> target)
    {
        var path = GetDacpacPath(builder.Resource);
        target.WithDacpac(path);
        builder.WaitFor(target);
        return builder;
    }

    private static string GetDacpacPath(SqlProjectResource resource)
    {
        if (resource.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
        {
            var projectPath = projectMetadata.ProjectPath;
            using var projectCollection = new ProjectCollection();
            var project = projectCollection.LoadProject(projectPath);

            // .sqlprojx has a SqlTargetPath property, so try that first
            var targetPath = project.GetPropertyValue("SqlTargetPath");
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                targetPath = project.GetPropertyValue("TargetPath");
            }

            return targetPath;
        }

        if (resource.TryGetLastAnnotation<DacpacMetadataAnnotation>(out var dacpacMetadata))
        {
            return dacpacMetadata.DacpacPath;
        }

        throw new InvalidOperationException($"Unable to locate SQL Server Database project package for resource {resource.Name}.");
    }
}