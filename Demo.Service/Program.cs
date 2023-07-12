using Amazon.S3;
using Demo;
using Demo.Abstractions;
using Demo.Controllers;
using Microsoft.OpenApi.Models;
using EventSourcing.Backbone;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ###############  EVENT SOURCING CONFIGURATION STARTS ############################

var services = builder.Services;

// inject AWS credentials according to the profile definition at appsettings.json
// Remember to set it right!
// see: https://medium.com/r/?url=https%3A%2F%2Fcodewithmukesh.com%2Fblog%2Faws-credentials-for-dotnet-applications%2F
services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
services.AddAWSService<IAmazonS3>();

builder.AddOpenTelemetryEventSourcing();
// Tune telemetry level
services.AddSingleton<TelemetryLevel>(LogLevel.Information);
// services.AddSingleton(new TelemetryLevel { Metric = LogLevel.Information, Trace = LogLevel.Debug });

services.AddEventSourceRedisConnection();
builder.AddKeyedJobOfferProducer(JobOfferConstants.URI, JobOfferConstants.S3_BUCKET);
builder.AddKeyedConsumer(JobOfferConstants.URI, JobOfferConstants.S3_BUCKET);

services.AddHostedService<ConsumerJob>();


// ###############  EVENT SOURCING CONFIGURATION ENDS ############################

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    opt =>
    {
        opt.SupportNonNullableReferenceTypes();
        opt.IgnoreObsoleteProperties();
        opt.IgnoreObsoleteActions();
        opt.DescribeAllParametersInCamelCase();

        opt.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "Environment setup",
            Description = 
            """
            Check the ReadMe for setting up developer environment.
            cd ./dockers/compose
            docker compose up -d
            Jaeger:  http://localhost:16686/search
            Grafana: http://localhost:3000
            """,
        });
    }); 

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var logger = app.Services.GetService<ILogger<Program>>();
List<string> switches = new();
switches.Add("Producer");
switches.Add("Consumer");
switches.Add("S3 Storage [Bucket:event-sourcing-demo, Profile:playground, Region:us-east-1]");
logger?.LogInformation("Service Configuration Event Sourcing `{event-bundle}` on URI: `{URI}`, Features: [{features}]", "JobOffer", JobOfferConstants.URI, string.Join(", ", switches));
    
app.Run();
