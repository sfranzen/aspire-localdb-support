using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var localDb = builder.AddSqlLocalDb("TestDb").WithOptions(options => options.AutomaticallyDeleteInstanceFiles = true);
var db = localDb.AddDatabase("Database", "Database1");
builder.AddSqlProject<Database1>("Database1").WithReference(db);
builder.Build().Run();