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
            app.UseStaticFiles("htdocs");
        }
    }
}