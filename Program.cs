using api.Extensions;
using api.Middleware;
using Mapster;
using System.Reflection;

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
            .AllowAnyMethod();
        // Pas de AllowCredentials avec AllowAnyOrigin !
    });
});

// Mapster : scan des configs IRegister dans l'assembly
TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());


// --- PIPELINE ---
var app = builder.Build();

// ✅ Ajout du log Quartz au démarrage
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogQuartzConfig(builder.Configuration);

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var isProcessPending = path.Contains("/process-pending", StringComparison.OrdinalIgnoreCase);

    if (isProcessPending)
    {
        var hasAuthorizationHeader = context.Request.Headers.ContainsKey("Authorization");
        logger.LogInformation(
            "➡️ Requête {Method} {Path} | AuthHeader={HasAuthHeader} | IsAuthenticated={IsAuthenticated}",
            context.Request.Method,
            path,
            hasAuthorizationHeader,
            context.User?.Identity?.IsAuthenticated ?? false);
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