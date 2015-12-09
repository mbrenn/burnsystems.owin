﻿using Owin;

namespace BurnSystems.Owin.StaticFiles
{
    public static class Integration
    {
        public static void UseStaticFiles(this IAppBuilder app, string directory)
        {
            var configuration = new StaticFileConfiguration()
            {
                Directory = directory
            };

            app.Use(typeof(StaticFilesMiddleware), configuration);
        }
    }
}