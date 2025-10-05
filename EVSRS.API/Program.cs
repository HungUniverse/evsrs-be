using System.Text.Json.Serialization;
using EVSRS.API.Constant;
using EVSRS.API.DependencyInjection;
using EVSRS.API.Middlewares;
using EVSRS.Repositories.Helper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NLog;
using NLog.Web;

namespace EVSRS.API
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var logger = LogManager.Setup().LoadConfigurationFromFile(string.Concat(Directory.GetCurrentDirectory(), "/nlog.config")).GetCurrentClassLogger();
            logger.Info($"================ APP STARTUP AT {DateTime.Now:yyyy-MM-dd HH:mm:ss} ================");
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Logging.ClearProviders();
                builder.Host.UseNLog();
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy(name: CorsConstant.PolicyName,
                        policy => { policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod(); });
                });

                builder.Services.AddControllers().AddJsonOptions(x =>
                {
                    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

                builder.Services.AddDatabase(builder.Configuration);
                builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddApplicationServices(builder.Configuration);
                builder.Services.AddHttpClientServices();
                builder.Services.AddConfigSwagger();
                builder.Services.AddJwtAuthentication(builder.Configuration);
                builder.Services.AddHealthChecks()
                    .AddCheck("self", () => HealthCheckResult.Healthy());
                var app = builder.Build();


                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                else
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EVSRS API V1");
                        c.RoutePrefix = string.Empty;
                    });
                }

                app.UseMiddleware<ExceptionMiddleware>();

                app.UseHttpsRedirection();

                app.UseRouting();

                app.UseCors(CorsConstant.PolicyName);

                app.UseSwagger();

                app.UseAuthentication();

                app.UseAuthorization();

                app.MapHealthChecks("/health");

                app.MapControllers();

                app.Run();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Stop program because of exception");
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}