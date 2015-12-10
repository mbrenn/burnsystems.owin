using BurnSystems.Owin.StaticFiles;
using Owin;

namespace OwinTesting
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
#if DEBUG
            app.UseErrorPage();
#endif
            var configuration = new StaticFilesConfiguration("htdocs");
            configuration.AddIgnoredExtension(".ts");

            app.UseStaticFiles(configuration);
        }
    }
}