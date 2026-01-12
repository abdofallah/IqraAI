using IqraCore.Entities.App.Lifecycle;
using IqraInfrastructure.Managers.App;

namespace ProjectIqraFrontend.Middlewares
{
    public class InstallationMiddleware
    {
        private readonly RequestDelegate _next;

        public InstallationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IqraAppManager appManager)
        {
            var path = context.Request.Path.Value?.ToLower();

            // 1. Whitelist logic (same as before)
            if (IsIgnoredPath(path))
            {
                await _next(context);
                return;
            }

            // 2. Check Cached Status (Instant)
            var status = appManager.CurrentStatus;

            // 3. Handle Not Installed
            if (status == AppLifecycleStatus.NotInstalled)
            {
                context.Response.Redirect("/install");
                return;
            }

            await _next(context);
        }

        private bool IsIgnoredPath(string? path)
        {
            if (path == null) return false;
            return path.StartsWith("/install") ||
                   path.StartsWith("/api/install") ||
                   path.Contains("/lib/") ||
                   path.Contains("/js/") ||
                   path.Contains("/img/") ||
                   path.Contains("/css/");
        }
    }
}
