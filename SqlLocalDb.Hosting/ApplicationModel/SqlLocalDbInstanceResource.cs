using MartinCostello.SqlLocalDb;

namespace Aspire.Hosting.ApplicationModel;

public class SqlLocalDbInstanceResource(string instanceName) : Resource(instanceName), IResourceWithConnectionString, IResourceWithWaitSupport
{
    private readonly Dictionary<string, string> _databases = [];

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($@"Data Source=(LocalDb)\{Name}");

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken ct = default) => ConnectionStringExpression.GetValueAsync(ct);

    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal bool AddDatabase(string name, string databaseName) => _databases.TryAdd(name, databaseName);
}