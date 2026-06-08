var builder = DistributedApplication.CreateBuilder(args);



var softwareApi = builder.AddProject<Projects.Software_Api>("software-api");

builder.Build().Run();
