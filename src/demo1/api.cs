#:sdk Microsoft.NET.Sdk.Web 



var builder = WebApplication.CreateBuilder(args);

// Configuring service

var app = builder.Build();

// Configuring Middleware
app.MapGet("/message", () => "Groovy");


app.Run(); // Run and listen for requests.