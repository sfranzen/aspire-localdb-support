# Introduction 
This is a sample .NET Aspire project showcasing a custom integration for LocalDb, which allows you to manage and deploy to LocalDb instances.

The integration is demonstrated by a small AppHost [Program.cs](/AppHost1/Program.cs) example. If you already have some familiarity with Aspire applications, the syntax should look familiar:
```cs
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var localDb = builder.AddLocalDbInstance("TestDb");              // Register a named LocalDb instance
var db = localDb.AddDatabase("Database", "Database1");           // Add a database resource to the instance
builder.AddSqlProject<Database1>("Database1").WithReference(db); // Populate the database using the dacpac produced by the SQL project
builder.Build().Run();
```

# Build and Test
The project can be built from the command line by running the following command from the root directory:
`dotnet run --project .\AppHost1\AppHost1.csproj`

Of course it should also work using your favourite IDE.

# Related work
In addition to the capabilities already provided with Aspire 9.0, the integration uses these NuGet packages:
* [CommunityToolkit.Aspire.Hosting.SqlDatabaseProjects](https://github.com/CommunityToolkit/Aspire) for the nonstandard `SqlProjectResource` abstraction;
* [MartinCostello.SqlLocalDb](https://github.com/martincostello/sqllocaldb) to manage LocalDb instances;
* [Microsoft.SqlServer.DacFx](https://github.com/microsoft/DacFx) for deploying dacpac files to databases.
