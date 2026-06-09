var builder = DistributedApplication.CreateBuilder(args);

var pg = builder.AddPostgres("pg-server")
    .WithLifetime(ContainerLifetime.Persistent);

var employeesDb = pg.AddDatabase("employees");


var employeesApi = builder.AddProject<Projects.Employees_Api>("employees-api")
    .WaitFor(employeesDb)
    .WithReference(employeesDb);

builder.Build().Run();