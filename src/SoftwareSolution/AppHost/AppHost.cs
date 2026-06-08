using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var pgServer = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithImageTag("17"); // make this as close to version you are using in prod as you can (dev/prod parity);

var softwareDb = pgServer.AddDatabase("software");

var scalar = builder.AddScalarApiReference(options =>
{
    options.PreferHttpsEndpoint = true;
    options.AllowSelfSignedCertificates = true;
    
});


var softwareApi = builder.AddProject<Projects.Software_Api>("software-api")
    .WithReference(softwareDb)
    .WaitFor(softwareDb);

scalar.WithApiReference(softwareApi);

builder.Build().Run();
