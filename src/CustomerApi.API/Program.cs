using CustomerApi.API;
using CustomerApi.Application;
using CustomerApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation(builder.Configuration);

var app = builder.Build();

app.MigrateDatabase();

app.UsePresentation();
app.MapControllers();
app.Run();

public partial class Program { }
