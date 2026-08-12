using api.Data;
using api.Interfaces;
using api.Models;
using api.Repository;
using api.Rules;
using api.Services;
using api.Configuration;
using api.Middleware;
using api.Services.Pdf;
using api.Services.Pdf.Templates;
using api.Services.Documents.Core;
using api.Services.Documents.Definitions;
using api.Services.Documents.Providers;
using api.Services.Documents.Renderers;
using api.Services.LegalDocuments;
using api.Services.Cmdb;
using api.Services.Workflow;
using api.Services.TaxReceipts;
using api.Services.Payments;
using api.Services.Jobs;
using api.Services.PersonalDashboard;
using api.Services.EuroFunds;
using api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Quartz;
using System.Text;
using System.Text.Json.Serialization;
using api.Interfaces.Documents;

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
        public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration config)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllHeaders", policy =>
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders("Content-Disposition"));

                var publicOrigins = config
                    .GetSection(PublicOriginOptions.SectionName)
                    .Get<PublicOriginOptions>();
                var allowedOrigins = publicOrigins?.Experiences.Values
                    .SelectMany(x => new[] { x.Origin }
                        .Concat(x.Domains.Select(domain => $"https://{domain}")))
                    .Select(origin => origin?.Trim().TrimEnd('/'))
                    .Where(origin => !string.IsNullOrWhiteSpace(origin))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? [];

                options.AddPolicy("ConfiguredCors", policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins!)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders("Content-Disposition");
                    }
                    else
                    {
                        policy.AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders("Content-Disposition");
                    }
                });
            });
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
                c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

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
            services.AddHttpContextAccessor();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config["Jwt:Issuer"],
                        ValidAudience = config["Jwt:Audience"],
                        NameClaimType = "username",
                        RoleClaimType = "role",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(config["Jwt:Key"]
                                ?? throw new ArgumentNullException("Jwt:Key")))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtAuth");

                            logger.LogWarning(
                                context.Exception,
                                "🔐 JWT AuthenticationFailed | Path={Path} | Message={Message}",
                                context.HttpContext.Request.Path,
                                context.Exception.Message);

                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtAuth");

                            logger.LogWarning(
                                "🔐 JWT Challenge | Path={Path} | Error={Error} | Description={Description}",
                                context.HttpContext.Request.Path,
                                context.Error,
                                context.ErrorDescription);

                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtAuth");

                            logger.LogWarning(
                                "🔐 JWT Forbidden | Path={Path} | User={User}",
                                context.HttpContext.Request.Path,
                                context.HttpContext.User?.Identity?.Name ?? "anonymous");

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = async context =>
                        {
                            var userIdValue = context.Principal?.FindFirst("userId")?.Value;
                            var sessionVersionValue = context.Principal?.FindFirst("sessionVersion")?.Value;

                            if (!int.TryParse(userIdValue, out var userId)
                                || !int.TryParse(sessionVersionValue, out var tokenSessionVersion))
                            {
                                context.Fail("Token de session invalide.");
                                return;
                            }

                            var db = context.HttpContext.RequestServices
                                .GetRequiredService<ApplicationDBContext>();
                            var user = await db.Users
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Id == userId);

                            if (user == null || user.SessionVersion != tokenSessionVersion)
                            {
                                context.Fail("Session révoquée.");
                                return;
                            }

                            if (user.Status is UserStatus.Suspended or UserStatus.Revoked or UserStatus.Locked)
                            {
                                context.Fail("Compte non autorisé.");
                            }
                        }
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicies.ViewUsers, policy =>
                    policy.RequireClaim("permission", SystemPermissions.UsersView));
                options.AddPolicy(AuthorizationPolicies.ManageUsers, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("permission", SystemPermissions.UsersCreate)
                        || context.User.HasClaim("permission", SystemPermissions.UsersUpdate)));
                options.AddPolicy(AuthorizationPolicies.ManageUserRoles, policy =>
                    policy.RequireClaim("permission", SystemPermissions.UsersAssignRoles));
                options.AddPolicy(AuthorizationPolicies.SuspendUsers, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("permission", SystemPermissions.UsersSuspend)
                        || context.User.HasClaim("permission", SystemPermissions.UsersReactivate)));
                options.AddPolicy(AuthorizationPolicies.RevokeUsers, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("permission", SystemPermissions.UsersRevoke)
                        || context.User.HasClaim("permission", SystemPermissions.UsersRestore)));
                options.AddPolicy(AuthorizationPolicies.ViewRoles, policy =>
                    policy.RequireClaim("permission", SystemPermissions.RolesView));
                options.AddPolicy(AuthorizationPolicies.ManageRoles, policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("permission", SystemPermissions.RolesCreate)
                        || context.User.HasClaim("permission", SystemPermissions.RolesUpdate)
                        || context.User.HasClaim("permission", SystemPermissions.RolesDelete)
                        || context.User.HasClaim("permission", SystemPermissions.RolesAssignPermissions)));
                options.AddPolicy(AuthorizationPolicies.ViewSecurityAudit, policy =>
                    policy.RequireClaim("permission", SystemPermissions.AuditView));

                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            return services;
        }

        // --- CONFIG DB CONTEXT ---
        public static IServiceCollection AddApiDbContext(this IServiceCollection services, IConfiguration config)
        {
            var connection = config.GetConnectionString("DefaultConnection");
            services.AddSingleton<SqlServerSessionOptionsInterceptor>();

            // Pool utilisé partout (thread-safe)
            services.AddPooledDbContextFactory<ApplicationDBContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(
                    connection,
                    sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                );
                options.AddInterceptors(serviceProvider.GetRequiredService<SqlServerSessionOptionsInterceptor>());
            });

            // Fournit un contexte “normal” via la fabrique
            services.AddScoped<ApplicationDBContext>(p =>
                p.GetRequiredService<IDbContextFactory<ApplicationDBContext>>().CreateDbContext());

            return services;
        }


        // --- DEPENDENCIES & SERVICES ---
        public static IServiceCollection AddApiDependencies(this IServiceCollection services, IConfiguration config)
        {

            QuestPDF.Settings.License = LicenseType.Community;
            services.AddMemoryCache();
            services
                .AddOptions<PublicOriginOptions>()
                .Bind(config.GetSection(PublicOriginOptions.SectionName))
                .Validate(o => o.Experiences.ContainsKey(o.DefaultExperience), "PublicOrigins:DefaultExperience doit exister dans Experiences")
                .Validate(o => o.Experiences.Values.All(x => Uri.TryCreate(x.Origin, UriKind.Absolute, out _)), "Chaque PublicOrigins:Experiences:*:Origin doit etre une URL absolue")
                .ValidateOnStart();
            services.AddScoped<IPublicOriginResolver, PublicOriginResolver>();

            services.AddScoped<IPersonRepository, PersonRepository>();
            services.AddScoped<IInsurerRepository, InsurerRepository>();
            services.AddScoped<INotaryRepository, NotaryRepository>();
            services.AddScoped<ICompartmentRepository, CompartmentRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductCatalogRepository, ProductCatalogRepository>();
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
            services.AddScoped<IAdvanceRepository, AdvanceRepository>();
            services.AddScoped<IAdvanceOperationService, AdvanceOperationService>();
            services.AddScoped<ISupportHistoricalDataRepository, SupportHistoricalDataRepository>();
            services.AddScoped<IContractOptionTypeRepository, ContractOptionTypeRepository>();
            services.AddScoped<FinancialSupportImportService>();
            services.AddScoped<IYahooFinanceProvider, YahooFinanceProvider>();
            services.AddScoped<IOperationRepository, OperationRepository>();
            services.AddHttpClient<IEodDataProvider, EodDataProvider>();
            services.AddHttpClient<ITwelveDataProvider, TwelveDataProvider>();
            services.AddScoped<IContractValuationService, ContractValuationService>();
            services.AddScoped<ICostBasisService, CostBasisService>();
            services.AddScoped<EuroFundAccrualCalculator>();
            services.AddScoped<IEuroFundLotService, EuroFundLotService>();
            services.AddScoped<IEuroFundValuationService, EuroFundValuationService>();
            services.AddScoped<IEuroFundRevaluationService, EuroFundRevaluationService>();
            services.AddScoped<IContractAuditService, ContractAuditService>();
            services.AddScoped<IFeeEngine, FeeEngine>();
            services.AddScoped<IManagementFeePolicyResolver, ManagementFeePolicyResolver>();
            services.AddScoped<IOperationFeePolicyResolver, OperationFeePolicyResolver>();
            services.AddScoped<RuleFactory>();
            services.AddScoped<BusinessRuleValidator>();
            services.AddScoped<IOperationEngineService, OperationEngineService>();
            services.AddScoped<IContractSupportHoldingRepository, ContractSupportHoldingRepository>();
            services.AddScoped<IFinancialSupportImportService, FinancialSupportImportService>();
            services.AddScoped<EodBulkImportService>();
            services.AddScoped<IJob, EodBulkImportJob>();
            services.AddScoped<UpdateValuationsJob>();
            services.AddScoped<IOperationApplier, OperationApplier>();
            services.AddScoped<ITaxProfileRepository, TaxProfileRepository>();
            services.AddScoped<ITaxEngineService, TaxEngineService>();
            services.AddScoped<IPdfDocumentService, PdfDocumentService>();
            services.AddScoped<IPdfBusinessDocumentService, PdfBusinessDocumentService>();
            services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
            services.AddScoped<IDocumentDefinitionRegistry, DocumentDefinitionRegistry>();
            services.AddScoped<ContractSituationDocumentDataProvider>();
            services.AddScoped<ContractSituationQuestPdfRenderer>();
            services.AddScoped<InformationSystemCartographyDataProvider>();
            services.AddScoped<InformationSystemCartographyQuestPdfRenderer>();
            services.AddScoped<LegalDocumentRevisionDataProvider>();
            services.AddScoped<LegalDocumentHtmlPdfRenderer>();
            services.AddScoped<TaxReceiptDocumentDataProvider>();
            services.AddScoped<TaxReceiptPdfRenderer>();
            services.AddScoped<ClientCaseFileDocumentDataProvider>();
            services.AddScoped<ClientCaseFilePdfMergeRenderer>();
            services.AddScoped<ContractSheetDocumentDataProvider>();
            services.AddScoped<ContractSheetPdfRenderer>();
            services.AddScoped<OperationsHistoryDocumentDataProvider>();
            services.AddScoped<OperationsHistoryPdfRenderer>();
            services.AddScoped<AssetAllocationReportDocumentDataProvider>();
            services.AddScoped<AssetAllocationReportPdfRenderer>();
            services.AddScoped<BoostSimulationDocumentDataProvider>();
            services.AddScoped<BoostSimulationHtmlPdfRenderer>();
            services.AddSingleton(new DocumentDefinition(
                "contract-situation",
                "Situation du contrat",
                "questpdf-contract-situation-v1",
                "Situation_contrat_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(ContractSituationDocumentDataProvider),
                typeof(ContractSituationQuestPdfRenderer),
                DocumentRenderEngine.QuestPdf,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 12,
                    MarginRightMm = 12,
                    MarginBottomMm = 12,
                    MarginLeftMm = 12
                }));
            services.AddSingleton(new DocumentDefinition(
                "information-system-cartography",
                "Cartographie du SI",
                "questpdf-cmdb-cartography-v1",
                "Cartographie_SI_{subjectId}_{date}.pdf",
                "A3",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(InformationSystemCartographyDataProvider),
                typeof(InformationSystemCartographyQuestPdfRenderer),
                DocumentRenderEngine.QuestPdf,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A3",
                    Orientation = "Portrait",
                    MarginTopMm = 10,
                    MarginRightMm = 15,
                    MarginBottomMm = 10,
                    MarginLeftMm = 15
                }));
            services.AddSingleton(new DocumentDefinition(
                "legal-document-revision",
                "Document juridique",
                "html-legal-document-v1",
                "Document_juridique_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(LegalDocumentRevisionDataProvider),
                typeof(LegalDocumentHtmlPdfRenderer),
                DocumentRenderEngine.HtmlToPdf,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 18,
                    MarginRightMm = 16,
                    MarginBottomMm = 18,
                    MarginLeftMm = 16
                }));
            services.AddSingleton(new DocumentDefinition(
                "tax-receipt",
                "Reçu fiscal",
                "pdf-template-tax-receipt-v1",
                "Recu_fiscal_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(TaxReceiptDocumentDataProvider),
                typeof(TaxReceiptPdfRenderer),
                DocumentRenderEngine.PdfTemplateOverlay,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 0,
                    MarginRightMm = 0,
                    MarginBottomMm = 0,
                    MarginLeftMm = 0,
                    PrintBackground = true,
                    PreferCssPageSize = false,
                    DisplayHeaderFooter = false
                }));
            services.AddSingleton(new DocumentDefinition(
                "contract-sheet",
                "Fiche contrat",
                "pdf-contract-sheet-v1",
                "Fiche_contrat_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(ContractSheetDocumentDataProvider),
                typeof(ContractSheetPdfRenderer),
                DocumentRenderEngine.QuestPdf,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 12,
                    MarginRightMm = 12,
                    MarginBottomMm = 12,
                    MarginLeftMm = 12,
                    PrintBackground = true,
                    PreferCssPageSize = false,
                    DisplayHeaderFooter = true
                }));
            services.AddSingleton(new DocumentDefinition(
                "operations-history",
                "Historique des opérations",
                "pdf-operations-history-v1",
                "Historique_operations_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(OperationsHistoryDocumentDataProvider),
                typeof(OperationsHistoryPdfRenderer),
                DocumentRenderEngine.QuestPdf,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 12,
                    MarginRightMm = 12,
                    MarginBottomMm = 12,
                    MarginLeftMm = 12,
                    PrintBackground = true,
                    PreferCssPageSize = false,
                    DisplayHeaderFooter = true
                }));
            services.AddSingleton(new DocumentDefinition(
                "asset-allocation-report",
                "Rapport d'allocation d'actifs",
                "pdf-asset-allocation-report-v1",
                "Allocation_actifs_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(AssetAllocationReportDocumentDataProvider),
                typeof(AssetAllocationReportPdfRenderer),
                DocumentRenderEngine.QuestPdf,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 12,
                    MarginRightMm = 12,
                    MarginBottomMm = 12,
                    MarginLeftMm = 12,
                    PrintBackground = true,
                    PreferCssPageSize = false,
                    DisplayHeaderFooter = true
                }));
            services.AddSingleton(new DocumentDefinition(
                "client-case-file",
                "Dossier client",
                "pdf-merge-client-case-file-v1",
                "Dossier_client_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(ClientCaseFileDocumentDataProvider),
                typeof(ClientCaseFilePdfMergeRenderer),
                DocumentRenderEngine.PdfMerge,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 12,
                    MarginRightMm = 12,
                    MarginBottomMm = 12,
                    MarginLeftMm = 12,
                    PrintBackground = true,
                    PreferCssPageSize = false,
                    DisplayHeaderFooter = true
                }));
            services.AddSingleton(new DocumentDefinition(
                "boost-simulation",
                "Simulation Boost",
                "html-boost-simulation-v1",
                "Simulation_Boost_{subjectId}_{date}.pdf",
                "A4",
                "Portrait",
                null,
                SupportsPreview: true,
                SupportsDownload: true,
                SupportsArchive: false,
                SupportsEmail: false,
                typeof(BoostSimulationDocumentDataProvider),
                typeof(BoostSimulationHtmlPdfRenderer),
                DocumentRenderEngine.HtmlToPdf,
                DocumentRenderOptions.Default with
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    MarginTopMm = 16,
                    MarginRightMm = 14,
                    MarginBottomMm = 16,
                    MarginLeftMm = 14,
                    PrintBackground = true,
                    PreferCssPageSize = true,
                    DisplayHeaderFooter = true
                }));
            services.AddScoped<IDocumentStructureService, DocumentStructureService>();
            services.AddScoped<ILegalDocumentImportService, LegalDocumentImportService>();
            services.AddScoped<IDocumentNumberingService, DocumentNumberingService>();
            services.AddScoped<IDocumentVersioningService, DocumentVersioningService>();
            services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
            services.AddScoped<IDocumentValidationService, DocumentValidationService>();
            services.AddScoped<IDocumentRenderService, DocumentRenderService>();
            services.AddScoped<IPdfGenerationService, PdfGenerationService>();
            services.AddScoped<IDocumentVariableResolver, DocumentVariableResolver>();
            services.AddScoped<IDocumentConditionEvaluator, DocumentConditionEvaluator>();
            services.AddScoped<IClauseCatalogService, ClauseCatalogService>();
            services.AddScoped<IDocumentComparisonService, DocumentComparisonService>();
            services.AddScoped<IDocumentAuditService, DocumentAuditService>();
            services.AddScoped<IDocumentBinaryStorage, DocumentBinaryStorage>();
            services.AddScoped<IProductDocumentAssignmentService, ProductDocumentAssignmentService>();
            services.AddScoped<ICmdbImportService, CmdbImportService>();
            services.AddScoped<IArchiMateFlowImportService, ArchiMateFlowImportService>();
            services.AddScoped<ICartographyDocumentService, CartographyDocumentService>();
            services.AddScoped<IProcessValidationService, ProcessValidationService>();
            services.AddScoped<IWorkflowRuntimeService, WorkflowRuntimeService>();
            services.AddScoped<IDonorService, DonorService>();
            services.AddScoped<IDonationService, DonationService>();
            services.AddScoped<IBeneficiaryOrganizationService, BeneficiaryOrganizationService>();
            services.AddScoped<ITaxReceiptService, TaxReceiptService>();
            services.AddScoped<ITaxReceiptNumberGenerator, TaxReceiptNumberGenerator>();
            services.AddScoped<IAmountToWordsService, AmountToWordsService>();
            services.AddScoped<ITaxReceiptPdfGenerator, TaxReceiptPdfGenerator2041Rd>();
            services.AddScoped<ITaxReceiptEmailService, TaxReceiptEmailService>();
            services.AddScoped<IHelloAssoTokenProvider, HelloAssoTokenProvider>();
            services.AddScoped<IPaymentProvider, HelloAssoPaymentProvider>();
            services.AddScoped<IBankAccountProtector, BankAccountProtector>();
            services.AddScoped<IIbanValidator, IbanValidator>();
            services.AddScoped<IDonationPaidProcessor, DonationPaidProcessor>();
            services.AddScoped<IPaymentReconciliationService, PaymentReconciliationService>();
            services.AddScoped<IMeDonationPaymentService, MeDonationPaymentService>();
            services.AddScoped<IDonationReceiptAccessTokenService, DonationReceiptAccessTokenService>();
            services.AddScoped<ISmtpMailSender, SmtpMailSender>();
            services.AddScoped<IPublicDonationService, PublicDonationService>();
            services.AddScoped<IMeProfileService, MeProfileService>();
            services.AddScoped<IMeDonationsService, MeDonationsService>();
            services.AddScoped<DemoNewsProvider>();
            services.AddScoped<RssNewsProvider>();
            services.AddScoped<INewsProvider>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalFeedsOptions>>().Value;
                return string.Equals(options.News.Provider, "Rss", StringComparison.OrdinalIgnoreCase)
                    ? sp.GetRequiredService<RssNewsProvider>()
                    : sp.GetRequiredService<DemoNewsProvider>();
            });
            services.AddScoped<DemoFinancialMarketProvider>();
            services.AddScoped<EodFinancialMarketProvider>();
            services.AddScoped<IFinancialMarketProvider>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalFeedsOptions>>().Value;
                return string.Equals(options.FinancialMarkets.Provider, "Eod", StringComparison.OrdinalIgnoreCase)
                    ? sp.GetRequiredService<EodFinancialMarketProvider>()
                    : sp.GetRequiredService<DemoFinancialMarketProvider>();
            });
            services.AddScoped<INewsFeedService, NewsFeedService>();
            services.AddScoped<IFinancialMarketService, FinancialMarketService>();
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<IAuthenticationAccountService, AuthenticationAccountService>();
            services.AddScoped<IUserAdministrationService, UserAdministrationService>();
            services.AddScoped<ICurrentUserAccessService, CurrentUserAccessService>();
            services.AddScoped<IInseeGeoService, InseeGeoService>();
            services.AddScoped<IInseeSireneService, InseeSireneService>();
            services.AddScoped<ISubscriptionDraftService, SubscriptionDraftService>();
            services.AddScoped<ISubscriptionDocumentService, SubscriptionDocumentService>();
            services.AddScoped<ISubscriptionMfaService, SubscriptionMfaService>();
            services.AddScoped<ISubscriptionPaymentPreparationService, SubscriptionPaymentPreparationService>();
            services.AddScoped<ISubscriptionSignatureService, SubscriptionSignatureService>();
            services.AddScoped<AuthorizationSeedService>();
            services.AddScoped<FieldDescriptionSeedService>();
            services.AddScoped<SendTaxReceiptEmailJob>();
            services.AddSingleton<IPdfTemplate, BusinessPdfTemplate>();
            services.AddSingleton<IPdfTemplate, ContractSheetPdfTemplate>();
            services.AddHttpClient("pdf-assets");
            services.AddHttpClient("subscription-sms", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddHttpClient("twilio-verify", (sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SubscriptionOperationsOptions>>().Value;
                client.BaseAddress = new Uri(options.Mfa.TwilioVerify.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddHttpClient("docuseal-signature", (sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SubscriptionOperationsOptions>>().Value;
                client.BaseAddress = new Uri(options.Signature.DocuSeal.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddHttpClient("youtrust-signature", (sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SubscriptionOperationsOptions>>().Value;
                client.BaseAddress = new Uri(options.Signature.Youtrust.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddHttpClient("personal-dashboard-news", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LifePersonalDashboard/1.0");
            });
            services.AddHttpClient("insee-sirene", (sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InseeSireneOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });
            services.AddHttpClient("insee-geo", (sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InseeSireneOptions>>().Value;
                client.BaseAddress = new Uri(options.GeoBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });
            services.AddHttpClient("helloasso-auth", (sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HelloAssoOptions>>().Value;
                client.BaseAddress = new Uri(options.TokenBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
            });
            services.AddHttpClient("helloasso-api", (sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HelloAssoOptions>>().Value;
                client.BaseAddress = new Uri(options.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
            });

            // Hosted services
            services.AddHostedService<ContractValuationCronService>();
            services.AddHostedService<EodBulkImportCronService>();
            services.AddHostedService<PaymentWebhookBackgroundService>();

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
            services.Configure<MailSettings>(config.GetSection("MailSettings"));
            services.Configure<AuthenticationOptions>(config.GetSection("Authentication"));
            services
                .AddOptions<ExternalFeedsOptions>()
                .Bind(config.GetSection("ExternalFeeds"))
                .Validate(o => o.News.DefaultLimit is >= 1 and <= 12, "ExternalFeeds:News:DefaultLimit doit etre entre 1 et 12")
                .Validate(o => o.News.MaxLimit is >= 1 and <= 30, "ExternalFeeds:News:MaxLimit doit etre entre 1 et 30")
                .Validate(o => o.FinancialMarkets.Symbols.Length > 0, "ExternalFeeds:FinancialMarkets:Symbols doit contenir au moins un symbole")
                .ValidateOnStart();
            services
                .AddOptions<PaymentsOptions>()
                .Bind(config.GetSection(PaymentsOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => !o.PayPal.Enabled || !string.IsNullOrWhiteSpace(o.PayPal.ClientId), "Payments:PayPal:ClientId obligatoire si PayPal est actif")
                .Validate(o => !o.PayPal.Enabled || !string.IsNullOrWhiteSpace(o.PayPal.ClientSecret), "Payments:PayPal:ClientSecret obligatoire si PayPal est actif")
                .Validate(o => !o.PayPal.Enabled || !string.IsNullOrWhiteSpace(o.PayPal.WebhookId), "Payments:PayPal:WebhookId obligatoire si PayPal est actif")
                .Validate(o => !o.BankTransfersEnabled || !string.IsNullOrWhiteSpace(o.BankEncryptionKey), "Payments:BankEncryptionKey doit etre defini via user-secrets ou variable d'environnement avant d'activer les virements")
                .ValidateOnStart();
            services
                .AddOptions<HelloAssoOptions>()
                .Bind(config.GetSection(HelloAssoOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => !o.Enabled || o.HasAnyCredentials, "Payments:HelloAsso doit contenir des credentials globaux ou au moins un alias complet")
                .ValidateOnStart();
            services
                .AddOptions<DonationCheckoutOptions>()
                .Bind(config.GetSection(DonationCheckoutOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => o.MaxAmountEur >= o.MinAmountEur, "DonationCheckout:MaxAmountEur doit etre >= MinAmountEur")
                .ValidateOnStart();
            services
                .AddOptions<InseeSireneOptions>()
                .Bind(config.GetSection(InseeSireneOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Insee:BaseUrl doit etre une URL absolue")
                .Validate(o => Uri.TryCreate(o.GeoBaseUrl, UriKind.Absolute, out _), "Insee:GeoBaseUrl doit etre une URL absolue")
                .ValidateOnStart();
            services
                .AddOptions<SubscriptionOperationsOptions>()
                .Bind(config.GetSection(SubscriptionOperationsOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => !o.Mfa.Sms.Enabled || !string.IsNullOrWhiteSpace(o.Mfa.Sms.EndpointUrl), "SubscriptionOperations:Mfa:Sms:EndpointUrl obligatoire si le SMS est actif")
                .Validate(o => !o.Mfa.TwilioVerify.Enabled || !string.IsNullOrWhiteSpace(o.Mfa.TwilioVerify.ServiceSid), "SubscriptionOperations:Mfa:TwilioVerify:ServiceSid obligatoire si Twilio Verify est actif")
                .Validate(o => !o.Mfa.TwilioVerify.Enabled
                               || (!string.IsNullOrWhiteSpace(o.Mfa.TwilioVerify.ApiKey) && !string.IsNullOrWhiteSpace(o.Mfa.TwilioVerify.ApiSecret))
                               || (!string.IsNullOrWhiteSpace(o.Mfa.TwilioVerify.AccountSid) && !string.IsNullOrWhiteSpace(o.Mfa.TwilioVerify.AuthToken)),
                    "SubscriptionOperations:Mfa:TwilioVerify doit contenir ApiKey/ApiSecret ou AccountSid/AuthToken si actif")
                .Validate(o => !o.Signature.DocuSeal.Enabled || !string.IsNullOrWhiteSpace(o.Signature.DocuSeal.ApiKey), "SubscriptionOperations:Signature:DocuSeal:ApiKey obligatoire si DocuSeal est actif")
                .Validate(o => !o.Signature.Youtrust.Enabled || !string.IsNullOrWhiteSpace(o.Signature.Youtrust.ApiKey), "SubscriptionOperations:Signature:Youtrust:ApiKey obligatoire si Youtrust est actif")
                .Validate(o => o.Mfa.ChallengeLifetime >= TimeSpan.FromMinutes(2), "SubscriptionOperations:Mfa:ChallengeLifetime doit etre >= 2 minutes")
                .ValidateOnStart();

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
                    .WithCronSchedule("0 0 4 * * ?")); // 4h00 quotidien pour accrual journalier
            });

            services.AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
            });

            return services;
        }

    }
}
