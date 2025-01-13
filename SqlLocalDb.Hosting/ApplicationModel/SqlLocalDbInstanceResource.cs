using MartinCostello.SqlLocalDb;

namespace Aspire.Hosting.ApplicationModel;

public class SqlLocalDbInstanceResource(string instanceName, SqlLocalDbOptions options) : Resource(instanceName), IResourceWithConnectionString, IResourceWithWaitSupport
{
    private readonly Dictionary<string, string> _databases = [];

    public IReadOnlyDictionary<string, string> Databases => _databases;

    public SqlLocalDbOptions Options { get; } = options;

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($@"Data Source=(LocalDb)\{Name}");

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken ct = default) => ConnectionStringExpression.GetValueAsync(ct);

    internal bool AddDatabase(string name, string databaseName) => _databases.TryAdd(name, databaseName);
}