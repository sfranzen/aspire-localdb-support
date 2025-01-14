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

# Motivation
Aspire is primarily aimed at the development of distributed applications hosted in containers, and built-in support for this scenario is excellent. Although it is also possible to connect to existing SQL server instances out of the box, simply by using a connection string resource instead of a container, there is no support for setting up LocalDb instances. Since these could be a viable alternative to containers, a custom integration like this might be of interest to you.

# Related work
In addition to the capabilities already provided with Aspire 9.0, the integration uses these NuGet packages:
* [CommunityToolkit.Aspire.Hosting.SqlDatabaseProjects](https://github.com/CommunityToolkit/Aspire) for the nonstandard `SqlProjectResource` abstraction;
* [MartinCostello.SqlLocalDb](https://github.com/martincostello/sqllocaldb) to manage LocalDb instances;
* [Microsoft.SqlServer.DacFx](https://github.com/microsoft/DacFx) for deploying dacpac files to databases.
