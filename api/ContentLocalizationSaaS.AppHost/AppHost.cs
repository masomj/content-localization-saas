
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume("locflow-postgres-data")
    .WithPgAdmin();

var contentDb = postgres.AddDatabase("contentdb", "content_localization");

var api = builder
    .AddProject<Projects.ContentLocalizationSaaS_Api>("api")
    .WaitFor(contentDb)
    .WithReference(contentDb);

var frontend = builder.AddJavaScriptApp("frontend", "..\\..\\frontend", "dev")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithReference(api);

builder.Build().Run();

public partial class Program;
