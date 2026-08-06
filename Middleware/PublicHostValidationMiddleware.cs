using api.Configuration;
using api.Interfaces;

namespace api.Middleware
{
    public sealed class PublicHostValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<PublicHostValidationMiddleware> _logger;

        public PublicHostValidationMiddleware(
            RequestDelegate next,
            IHostEnvironment environment,
            ILogger<PublicHostValidationMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPublicOriginResolver resolver)
        {
            if (!_environment.IsDevelopment())
            {
                var resolved = resolver.ResolveCurrent();
                if (!resolved.IsKnownHost && resolved.UnknownHostPolicy == UnknownHostPolicy.Reject)
                {
                    _logger.LogWarning(
                        "Host public refuse: Host={Host} Path={Path}",
                        context.Request.Host.Value,
                        context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Host non autorise.");
                    return;
                }
            }

            await _next(context);
        }
    }
}

