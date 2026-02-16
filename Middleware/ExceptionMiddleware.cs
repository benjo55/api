using System.Net;
using System.Text.Json;
using api.Exceptions;

namespace api.Middleware
{
    // Middleware pour gérer les exceptions globalement
    // Intercepte les exceptions non gérées et renvoie une réponse JSON appropriée
    // Utilise BusinessException pour les erreurs métier (400 Bad Request)
    // et gère les autres exceptions comme des erreurs serveur (500 Internal Server Error)
    // Logge les erreurs avec ILogger
    // À ajouter dans le pipeline dans Program.cs avec app.UseMiddleware<ExceptionMiddleware>();    

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business exception");

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new { message = ex.Message });
                await context.Response.WriteAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new { message = "Une erreur interne est survenue." });
                await context.Response.WriteAsync(result);
            }
        }
    }
}