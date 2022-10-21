using Mentalist.Monitoring.Pods;
using Mentalist.Monitoring.Pods.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<MonitoringHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/status", () =>
    MetricsHostedServiceState.IsAlive() ? Results.StatusCode(200) : Results.StatusCode(500)
);

app.MapGet("/versions", () => Namespaces.All).WithName("GetVersions");

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();
});

app.Run();
