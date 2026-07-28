using api.Extensions;
using api.Interfaces;
using api.Middleware;
using Mapster;
using System.Reflection;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURATION DES SERVICES ---

builder.Services.AddApiCors()
    .AddApiControllers()
    .AddApiSwagger()
    .AddApiAuthentication(builder.Configuration)
    .AddApiDbContext(builder.Configuration)
    .AddApiDependencies(builder.Configuration)
    .AddQuartzJobs(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition");
        // Pas de AllowCredentials avec AllowAnyOrigin !
    });
});

// Mapster : scan des configs IRegister dans l'assembly
TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());


// --- PIPELINE ---
var app = builder.Build();

var cartographyExportArgumentIndex = Array.FindIndex(
    args,
    value => string.Equals(
        value,
        "--export-cartography-document",
        StringComparison.OrdinalIgnoreCase));
if (cartographyExportArgumentIndex >= 0)
{
    if (cartographyExportArgumentIndex + 2 >= args.Length)
    {
        throw new ArgumentException(
            "L'entité et le chemin de sortie doivent suivre --export-cartography-document.");
    }

    using var scope = app.Services.CreateScope();
    var generator = scope.ServiceProvider
        .GetRequiredService<api.Services.Cmdb.ICartographyDocumentService>();
    var result = await generator.GenerateAsync(
        args[cartographyExportArgumentIndex + 1],
        cancellationToken: CancellationToken.None);
    if (result is null)
    {
        throw new InvalidOperationException(
            "Aucune application métier active n'a été trouvée pour cette entité.");
    }

    var outputPath = Path.GetFullPath(args[cartographyExportArgumentIndex + 2]);
    Directory.CreateDirectory(
        Path.GetDirectoryName(outputPath)
        ?? throw new InvalidOperationException("Chemin de sortie invalide."));
    await File.WriteAllBytesAsync(outputPath, result.Content);
    Console.WriteLine(
        $"Document cartographique généré : {outputPath} ({result.Content.Length} octets)");
    return;
}

var archiMateImportArgumentIndex = Array.FindIndex(
    args,
    value => string.Equals(value, "--import-archimate", StringComparison.OrdinalIgnoreCase));
if (archiMateImportArgumentIndex >= 0)
{
    if (archiMateImportArgumentIndex + 1 >= args.Length)
    {
        throw new ArgumentException("Le chemin du fichier doit suivre --import-archimate.");
    }

    using var scope = app.Services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<api.Services.Cmdb.IArchiMateFlowImportService>();
    var result = await importer.ImportAsync(args[archiMateImportArgumentIndex + 1], CancellationToken.None);
    Console.WriteLine(
        $"Import ArchiMate: relations détectées={result.DetectedRelationships}, flux détectés={result.DetectedFlows}, relations dynamiques importées={result.ImportedFlows}, flux créés={result.CreatedFlows}, flux mis-à-jour={result.UpdatedFlows}, relations structurelles importées={result.ImportedStructuralRelationships}, structurelles créées={result.CreatedStructuralRelationships}, structurelles mises-à-jour={result.UpdatedStructuralRelationships}, légende ignorée={result.SkippedLegendFlows}, extrémités={result.DistinctEndpoints}, rapprochées={result.MatchedConfigurationItems}, placeholders={result.PlaceholderConfigurationItems}");
    return;
}

var cmdbImportArgumentIndex = Array.FindIndex(
    args,
    value => string.Equals(value, "--import-cmdb", StringComparison.OrdinalIgnoreCase));
if (cmdbImportArgumentIndex >= 0)
{
    if (cmdbImportArgumentIndex + 1 >= args.Length)
    {
        throw new ArgumentException("Le chemin du répertoire CSV doit suivre --import-cmdb.");
    }

    using var scope = app.Services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<api.Services.Cmdb.ICmdbImportService>();
    var result = await importer.ImportDirectoryAsync(args[cmdbImportArgumentIndex + 1], CancellationToken.None);
    Console.WriteLine(
        $"Import CMDB: run={result.ImportRunId}, ci={result.ConfigurationItemCount}, relations={result.RelationshipCount}, attributs={result.AttributeCount}, supports={result.SupportAssignmentCount}, rejets={result.RejectedCount}");
    return;
}

var importArgumentIndex = Array.FindIndex(
    args,
    value => string.Equals(value, "--import-legal-document", StringComparison.OrdinalIgnoreCase));
if (importArgumentIndex >= 0)
{
    if (importArgumentIndex + 1 >= args.Length)
    {
        throw new ArgumentException("Le chemin du fichier JSON doit suivre --import-legal-document.");
    }

    using var scope = app.Services.CreateScope();
    var importer = scope.ServiceProvider.GetRequiredService<ILegalDocumentImportService>();
    var result = await importer.ImportAsync(args[importArgumentIndex + 1], "import-cli");
    var validationService = scope.ServiceProvider.GetRequiredService<IDocumentValidationService>();
    var validation = await validationService.ValidateRevisionAsync(
        result.RevisionId,
        includePdfGeneration: false);
    if (!validation.IsValid)
    {
        var messages = string.Join(
            Environment.NewLine,
            validation.Issues.Select(issue => $"- {issue.Code}: {issue.Message}"));
        throw new InvalidOperationException($"Le document importé est invalide :{Environment.NewLine}{messages}");
    }

    var structureService = scope.ServiceProvider.GetRequiredService<IDocumentStructureService>();
    var revision = await structureService.GetRevisionAsync(result.RevisionId)
        ?? throw new InvalidOperationException("La révision importée est introuvable.");
    var renderService = scope.ServiceProvider.GetRequiredService<IDocumentRenderService>();
    var preview = await renderService.GeneratePreviewAsync(
        result.RevisionId,
        revision.ContentHash ?? string.Empty,
        "import-cli");
    Console.WriteLine(
        $"Import documentaire: definition={result.DefinitionId}, revision={result.RevisionId}, nodes={result.NodeCount}, imported={result.Imported}, validation=ok, artifact={preview.ArtifactId}");
    return;
}

// ✅ Ajout du log Quartz au démarrage
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogQuartzConfig(builder.Configuration);

using (var scope = app.Services.CreateScope())
{
    var seedService = scope.ServiceProvider.GetRequiredService<api.Services.AuthorizationSeedService>();
    await seedService.SeedAsync();
}

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var isProcessPending = path.Contains("/process-pending", StringComparison.OrdinalIgnoreCase);

    if (isProcessPending)
    {
        var hasAuthorizationHeader = context.Request.Headers.ContainsKey("Authorization");

        string? tokenPreview = null;
        if (hasAuthorizationHeader)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var tk = authHeader.Substring("Bearer ".Length).Trim();
                tokenPreview = tk.Length > 8 ? tk.Substring(0, 8) + "..." : tk;
            }
        }

        logger.LogInformation(
            "➡️ Requête {Method} {Path} | AuthHeader={HasAuthHeader} | IsAuthenticated={IsAuthenticated} | TokenPreview={TokenPreview}",
            context.Request.Method,
            path,
            hasAuthorizationHeader,
            context.User?.Identity?.IsAuthenticated ?? false,
            tokenPreview);
    }

    await next();

    if (isProcessPending)
    {
        logger.LogInformation(
            "⬅️ Réponse {Method} {Path} | StatusCode={StatusCode}",
            context.Request.Method,
            path,
            context.Response.StatusCode);
    }
});

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

Console.WriteLine("✅ JWT Issuer: " + builder.Configuration["Jwt:Issuer"]);
Console.WriteLine("✅ JWT Audience: " + builder.Configuration["Jwt:Audience"]);

// Applique la policy CORS ultra permissive pour debug
app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
