using System.Diagnostics;
using Owin;

namespace BurnSystems.Owin.StaticFiles
{
    public static class Integration
    {
        public static void UseStaticFiles(this IAppBuilder app, string directory)
        {
            var configuration = new StaticFilesConfiguration()
            {
                Directory = directory
            };

            UseStaticFiles(app, configuration);
        }

        public static void UseStaticFiles(this IAppBuilder app, StaticFilesConfiguration configuration)
        {
            Debug.Assert(configuration != null, "configuration != null");

            app.Use(typeof(StaticFilesMiddleware), configuration);
        }
    }
}