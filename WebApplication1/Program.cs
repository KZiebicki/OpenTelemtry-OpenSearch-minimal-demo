
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Globalization;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            const string serviceName = "roll-dice";

            builder.Logging.AddOpenTelemetry(options =>
            {
                options
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri("http://localhost:4317");
                        otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    })
                    .AddConsoleExporter();
            });
            builder.Services.AddOpenTelemetry()
                  .ConfigureResource(resource => resource.AddService(serviceName))
                  .WithTracing(x =>
                  {
                      if (builder.Environment.IsDevelopment())
                          x.SetSampler<AlwaysOnSampler>();
                      x.AddAspNetCoreInstrumentation()
                      //.AddGrpcClientInstrumentation()
                      .AddHttpClientInstrumentation()
                      .AddOtlpExporter(otlpOptions =>
                      {
                          otlpOptions.Endpoint = new Uri("http://localhost:4317");
                          otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                      })
                      .AddConsoleExporter();
                  })
                      .WithMetrics(x =>
                      {
                          x.AddRuntimeInstrumentation()
                              .AddMeter(
                              "Microsoft.AspNetCore.Hosting",
                              "Microsoft.AspNetCore.Server.Kestrel",
                              "System.Net.Http")
                          .AddOtlpExporter(otlpOptions =>
                          {
                              otlpOptions.Endpoint = new Uri("http://localhost:4317");
                              otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                          })
                        .AddConsoleExporter();
                      });

            var app = builder.Build();

            string HandleRollDice([FromServices] ILogger<Program> logger, string? player)
            {
                var result = RollDice();

                if (string.IsNullOrEmpty(player))
                {
                    logger.LogInformation("Anonymous player is rolling the dice: {result}", result);
                }
                else
                {
                    logger.LogInformation("{player} is rolling the dice: {result}", player, result);
                }

                return result.ToString(CultureInfo.InvariantCulture);
            }

            int RollDice()
            {
                return Random.Shared.Next(1, 7);
            }

            app.MapGet("/rolldice/{player?}", HandleRollDice);


            app.Run();
        }
    }
}
