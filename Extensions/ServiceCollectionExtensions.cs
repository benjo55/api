using api.Data;
using api.Interfaces;
using api.Models;
using api.Repository;
using api.Rules;
using api.Services;
using api.Configuration;
using api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using System.Text;
using System.Text.Json.Serialization;

namespace api.Extensions
{
    public static class QuartzLoggingExtensions
    {
        public static void LogQuartzConfig(this ILogger logger, IConfiguration config)
        {
            var quartzConfig = config.GetSection("Quartz").AsEnumerable().Where(x => x.Value != null);

            logger.LogInformation("🔎 Quartz configuration chargée :");
            foreach (var kvp in quartzConfig)
            {
                logger.LogInformation($"   {kvp.Key} = {kvp.Value}");
            }
        }
    }

    public static class ServiceCollectionExtensions
    {
        // --- CONFIG CORS ---
        public static IServiceCollection AddApiCors(this IServiceCollection services)
        {
            services.AddCors(options =>
                options.AddPolicy("AllowAllHeaders", policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
                )
            );
            return services;
        }

        // --- CONFIG CONTROLLERS JSON ---
        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });
            return services;
        }

        // --- CONFIG SWAGGER ---
        public static IServiceCollection AddApiSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Entrer 'Bearer' suivi d'un espace et du token JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        new List<string>()
                    }
                });
            });
            return services;
        }

        // --- CONFIG AUTHENTICATION ---
        public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config["Jwt:Issuer"],
                        ValidAudience = config["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(config["Jwt:Key"]
                                ?? throw new ArgumentNullException("Jwt:Key")))
                    };
                });
            services.AddAuthorization();
            return services;
        }

        // --- CONFIG DB CONTEXT ---
        public static IServiceCollection AddApiDbContext(this IServiceCollection services, IConfiguration config)
        {
            var connection = config.GetConnectionString("DefaultConnection");

            // Pool utilisé partout (thread-safe)
            services.AddPooledDbContextFactory<ApplicationDBContext>(options =>
            {
                options.UseSqlServer(
                    connection,
                    sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                );
            });

            // Fournit un contexte “normal” via la fabrique
            services.AddScoped<ApplicationDBContext>(p =>
                p.GetRequiredService<IDbContextFactory<ApplicationDBContext>>().CreateDbContext());

            return services;
        }


        // --- DEPENDENCIES & SERVICES ---
        public static IServiceCollection AddApiDependencies(this IServiceCollection services, IConfiguration config)
        {

            services.AddScoped<IPersonRepository, PersonRepository>();
            services.AddScoped<IInsurerRepository, InsurerRepository>();
            services.AddScoped<INotaryRepository, NotaryRepository>();
            services.AddScoped<ICompartmentRepository, CompartmentRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IBeneficiaryClauseRepository, BeneficiaryClauseRepository>();
            services.AddScoped<IBeneficiaryClausePersonRepository, BeneficiaryClausePersonRepository>();
            services.AddScoped<IEntityHistoryRepository, EntityHistoryRepository>();
            services.AddScoped<IFieldDescriptionRepository, FieldDescriptionRepository>();
            services.AddScoped<EntityHistoryService>();
            services.AddScoped<IFinancialSupportRepository, FinancialSupportRepository>();
            services.AddScoped<ISupportHistoricalDataRepository, SupportHistoricalDataRepository>();
            services.AddScoped<IContractOptionTypeRepository, ContractOptionTypeRepository>();
            services.AddScoped<FinancialSupportImportService>();
            services.AddScoped<IYahooFinanceProvider, YahooFinanceProvider>();
            services.AddScoped<IOperationRepository, OperationRepository>();
            services.AddHttpClient<IEodDataProvider, EodDataProvider>();
            services.AddHttpClient<ITwelveDataProvider, TwelveDataProvider>();
            services.AddScoped<IContractValuationService, ContractValuationService>();
            services.AddScoped<RuleFactory>();
            services.AddScoped<BusinessRuleValidator>();
            services.AddScoped<IOperationEngineService, OperationEngineService>();
            services.AddScoped<IContractSupportHoldingRepository, ContractSupportHoldingRepository>();
            services.AddScoped<IFinancialSupportImportService, FinancialSupportImportService>();
            services.AddScoped<EodBulkImportService>();
            services.AddScoped<IJob, EodBulkImportJob>();
            services.AddScoped<UpdateValuationsJob>();
            services.AddScoped<IOperationApplier, OperationApplier>();

            // Hosted services
            services.AddHostedService<ContractValuationCronService>();
            services.AddHostedService<EodBulkImportCronService>();

            // Validation générique
            services.AddScoped(typeof(IValidationService<>), typeof(ValidationService<>));

            // AuthService singleton
            services.AddSingleton<AuthService>();

            // Règles métier génériques
            services.Scan(scan => scan
                .FromAssemblyOf<IBusinessRule<Person>>()
                .AddClasses(classes => classes.AssignableTo(typeof(IBusinessRule<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Configuration des options
            services.Configure<EodSettings>(config.GetSection("EodSettings"));

            return services;
        }

        // --- QUARTZ JOBS ---
        public static IServiceCollection AddQuartzJobs(this IServiceCollection services, IConfiguration config)
        {
            services.AddQuartz(q =>
            {
                // ThreadPool optionnel
                q.UseDefaultThreadPool(tp =>
                {
                    tp.MaxConcurrency = 10;
                });

                q.UsePersistentStore(store =>
                {
                    store.UseProperties = true;
                    store.RetryInterval = TimeSpan.FromSeconds(15);
                    store.UseSqlServer(sql =>
                    {
                        sql.ConnectionString = config.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("DefaultConnection", "Connection string 'DefaultConnection' not found.");
                    });
                    store.UseNewtonsoftJsonSerializer();
                });

                // Jobs Quartz

                var helloKey = new JobKey("HelloQuartz");
                q.AddJob<HelloQuartzJob>(opts => opts.WithIdentity(helloKey));
                q.AddTrigger(opts => opts
                    .ForJob(helloKey)
                    .WithIdentity("HelloQuartzTrigger")
                    .WithCronSchedule("0 0/1 * * * ?")); // toutes les minutes

                var updateValuationsKey = new JobKey("UpdateValuations");
                q.AddJob<UpdateValuationsJob>(opts => opts.WithIdentity(updateValuationsKey));
                q.AddTrigger(opts => opts
                    .ForJob(updateValuationsKey)
                    .WithIdentity("UpdateValuationsTrigger")
                    .WithCronSchedule("0 0 2 * * ?")); // 2h00

                var processOpsKey = new JobKey("ProcessPendingOperations");
                q.AddJob<ProcessPendingOperationsJob>(opts => opts.WithIdentity(processOpsKey));
                q.AddTrigger(opts => opts
                    .ForJob(processOpsKey)
                    .WithIdentity("ProcessPendingOperationsTrigger")
                    .WithCronSchedule("0 0 3 * * ?")); // 3h00

                var feesKey = new JobKey("ApplyManagementFees");
                q.AddJob<ApplyManagementFeesJob>(opts => opts.WithIdentity(feesKey));
                q.AddTrigger(opts => opts
                    .ForJob(feesKey)
                    .WithIdentity("ApplyManagementFeesTrigger")
                    .WithCronSchedule("0 0 2 1 * ?")); // 1er du mois 2h00
            });

            services.AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

            return services;
        }

    }
}
