var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin();

var contentDb = postgres.AddDatabase("contentdb", "content_localization");

var api = builder
    .AddProject<Projects.ContentLocalizationSaaS_Api>("api")
    .WithReference(contentDb)
    .WaitFor(contentDb);

builder
    .AddNpmApp("frontend", "..\\..\\frontend", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();

public partial class Program;
