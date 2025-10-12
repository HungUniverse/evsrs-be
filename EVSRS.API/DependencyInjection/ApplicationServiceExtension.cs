using System;
using System.Text;
using System.Text.Json;
using EVSRS.BusinessObjects.DBContext;
using EVSRS.Repositories.Helper;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using EVSRS.Repositories.Repository;
using EVSRS.Services.Interface;
using EVSRS.Services.Mapper;
using EVSRS.Services.Service;
using EVSRS.Services.ExternalServices.SepayService;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EVSRS.API.DependencyInjection
{
    public static class ApplicationServiceExtension
    {
        public static void AddRepositories(this IServiceCollection services)
        {
            // Register your repositories here
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOTPRepository, OTPRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<ICarManufactureRepository, CarManufactureRepository>();
            services.AddScoped<IModelRepository, ModelRepository>();
            services.AddScoped<ICarEVRepository, CarEVRepository>();
            services.AddScoped<IAmenitiesRepository, AmenitiesRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IDepotRepository, DepotRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IOrderBookingRepository, OrderBookingRepository>();
            services.AddScoped<IHandoverInspectionRepository, HandoverInspectionRepository>();
            services.AddScoped<IReturnSettlementRepository, ReturnSettlementRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IIdentifyDocumentRepository, IdentifyDocumentRepository>();

        }
        public static void AddServices(this IServiceCollection services)
        {
            // Register your services here
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOTPService, OTPService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IEmailSenderSevice, EmailSenderService>();
            services.AddScoped<ICarManufactureService, CarManufactureService>();
            services.AddScoped<IModelService, ModelService>();
            services.AddScoped<ICarEVService, CarEVService>();
            services.AddScoped<IAmenitiesService, AmenitiesService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IDepotService, DepotService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IOrderBookingService, OrderBookingService>();
            services.AddScoped<ISepayService, SepayService>();
            services.AddScoped<IHandoverService, HandoverService>();
            services.AddScoped<IReturnService, ReturnService>();
            services.AddScoped<IIdentifyDocumentService, IdentifyDocumentService>();




        }
        public static IServiceCollection AddHttpClientServices(this IServiceCollection services)
        {
            services.AddHttpClient();
            return services;
        }
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString)
            );
            return services;
        }
        public static IServiceCollection AddConfigSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo() { Title = "EVSRS System", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                options.EnableAnnotations();
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
                options.MapType<TimeOnly>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "time",
                    Example = OpenApiAnyFactory.CreateFromJson("\"13:45:42\"")
                });
                // options.OperationFilter<FileUploadOperationFiler>();
            });
            return services;
        }
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JWT");
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var secretKey = jwtSettings["SecretKey"];

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero // Tùy chỉnh: giảm thời gian lệch đồng hồ, cho chặt
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Read the token from query string for SignalR
                            var accessToken = context.Request.Query["access_token"];
                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/chatHub")) ||
                                path.StartsWithSegments("/notificationHub"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            var response = context.Response;
                            response.ContentType = "application/json";

                            string message = "Token is invalid";
                            string errorCode = ApiCodes.TOKEN_INVALID;
                            int statusCode = StatusCodes.Status401Unauthorized;

                            if (context.AuthenticateFailure is SecurityTokenExpiredException)
                            {
                                message = "Token is expired";
                                errorCode = ApiCodes.TOKEN_EXPIRED;
                                statusCode = StatusCodes.Status403Forbidden;
                            }
                            else if (string.IsNullOrEmpty(context.Request.Headers["Authorization"]))
                            {
                                message = "No token provided";
                                errorCode = ApiCodes.UNAUTHENTICATED;
                            }

                            response.StatusCode = statusCode;

                            var result = JsonSerializer.Serialize(new
                            {
                                errorCode,
                                message,
                            });

                            await response.WriteAsync(result);
                        }
                    };
                });

            return services;
        }
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg =>
                        ((int)msg.StatusCode >= 500 && (int)msg.StatusCode < 600) // chỉ retry 5xx
                )
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan} seconds, status={outcome.Result?.StatusCode}");
                    });
        }
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper();
            services.AddRepositories();
            services.AddServices();
            
            // Register configuration settings
            services.Configure<SepaySettings>(configuration.GetSection("SepaySettings"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        }
        
        private static void AddAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MapperEntities).Assembly);
        }
        
        
    }
}