using MartinCostello.SqlLocalDb;

namespace Aspire.Hosting.ApplicationModel;

public class LocalDbInstanceResource(string instanceName) : Resource(instanceName), IResourceWithConnectionString, IResourceWithWaitSupport
{
    private static readonly SqlLocalDbApi LocalDbApi = new();
    private readonly Dictionary<string, string> _databases = [];

    public ISqlLocalDbInstanceInfo Instance { get; } = LocalDbApi.GetOrCreateInstance(instanceName);

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{Instance.GetConnectionString()}");

    public ValueTask<string?> GetConnectionStringAsync(CancellationToken ct = default) => ConnectionStringExpression.GetValueAsync(ct); 
    
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal bool AddDatabase(string name, string databaseName) => _databases.TryAdd(name, databaseName);
}