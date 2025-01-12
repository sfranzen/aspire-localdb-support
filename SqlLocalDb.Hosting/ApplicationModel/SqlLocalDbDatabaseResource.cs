namespace Aspire.Hosting.ApplicationModel;

public class SqlLocalDbDatabaseResource(string name, string databaseName, SqlLocalDbInstanceResource instance) : Resource(name), IResourceWithParent<SqlLocalDbInstanceResource>, IResourceWithConnectionString
{
    public SqlLocalDbInstanceResource Parent { get; } = instance;

    public string DatabaseName { get; } = databaseName;

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken ct = default) => ConnectionStringExpression.GetValueAsync(ct);
}