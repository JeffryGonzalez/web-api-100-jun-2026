using Marten;
using Microsoft.AspNetCore.Connections.Features;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddNpgsqlDataSource(connectionName: "employees");

builder.Services.AddMarten(options =>
{

}).UseNpgsqlDataSource().UseLightweightSessions();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapControllers();
app.UseHttpsRedirection();


app.MapDefaultEndpoints();
app.Run();

