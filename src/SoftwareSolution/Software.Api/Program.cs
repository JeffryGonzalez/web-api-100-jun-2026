var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // ServiceDefaults Extension

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Above this line is host and service configuration - can't do that after you build the app.
var app = builder.Build();
app.MapDefaultEndpoints(); // ServiceDefaults - this is mostly health checks.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // GET /openapi/v1.json 
}

app.UseHttpsRedirection(); // If a request comes in using http, redirect them back to https. Turn off HTTP.

app.UseAuthorization(); // We'll talk about this.

app.MapControllers(); // Need to have the builder.Services.AddController() above, this uses reflection to create your routes.
// GET /status = StatusController Then call GetTheStatus and return to the user-agent whatever that returns.
app.Run(); // It is up and running, waiting for someone to call. 
