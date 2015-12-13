using System;
using System.IO;
using BurnSystems.Owin.StaticFiles.IIS;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace BurnSystems.Owin.StaticFiles.IIS
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var configuration = new StaticFilesConfiguration("htdocs");
            configuration.AddIgnoredExtension(".ts");

            app.UseStaticFiles(configuration);

            app.UseStageMarker(PipelineStage.MapHandler);
        }
    }
}