using api.Extensions;
using api.Middleware;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURATION DES SERVICES ---
builder.Services.AddApiCors()
                .AddApiControllers()
                .AddApiSwagger()
                .AddApiAuthentication(builder.Configuration)
                .AddApiDbContext(builder.Configuration)
                .AddApiDependencies(builder.Configuration)
                .AddQuartzJobs(builder.Configuration);

// ✅ FIX AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


// --- PIPELINE ---
var app = builder.Build();

// ✅ Ajout du log Quartz au démarrage
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogQuartzConfig(builder.Configuration);

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

Console.WriteLine("✅ JWT Issuer: " + builder.Configuration["Jwt:Issuer"]);
Console.WriteLine("✅ JWT Audience: " + builder.Configuration["Jwt:Audience"]);

app.UseCors("AllowAllHeaders");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();