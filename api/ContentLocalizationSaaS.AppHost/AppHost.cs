var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin();

var contentDb = postgres.AddDatabase("contentdb", "content_localization");

builder
    .AddProject<Projects.ContentLocalizationSaaS_Api>("api")
    .WithReference(contentDb)
    .WaitFor(contentDb);

builder.Build().Run();

public partial class Program;
