namespace Aspire.Hosting.ApplicationModel;

public class LocalDbDatabaseResource(string name, string databaseName, LocalDbInstanceResource instance) : Resource(name), IResourceWithParent<LocalDbInstanceResource>, IResourceWithConnectionString
{
    public LocalDbInstanceResource Parent { get; } = instance;

    public string DatabaseName { get; } = databaseName;

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken ct = default) => ConnectionStringExpression.GetValueAsync(ct);
}