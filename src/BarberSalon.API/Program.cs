using BarberSalon.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(BarberSalon.API.DependencyInjection.CorsPolicyName);

app.MapControllers();

app.Run();

/// <summary>Host entry point, exposed as public so it can be used by WebApplicationFactory integration tests.</summary>
public partial class Program { }
