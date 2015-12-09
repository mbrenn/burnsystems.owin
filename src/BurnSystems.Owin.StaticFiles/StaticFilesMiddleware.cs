using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace BurnSystems.Owin.StaticFiles
{
    /// <summary>
    /// Defines the owin middleware which serves the static files
    /// </summary>
    public class StaticFilesMiddleware : OwinMiddleware
    {
        public StaticFilesConfiguration Configuration { get; }

        public StaticFilesMiddleware(OwinMiddleware next, StaticFilesConfiguration configuration) : base(next)
        {
            Debug.Assert(configuration != null, "_configuration != null");

            if (!Directory.Exists(configuration.Directory))
            {
                throw new InvalidOperationException("Path for static file directory does not exist: " +
                                                    configuration.Directory);
            }

            Configuration = configuration;
        }

    /// <summary>
        /// Entry point for the request
        /// </summary>
        /// <param name="context">Owin context containing the information</param>
        /// <returns>Task for the invocation</returns>
        public override async Task Invoke(IOwinContext context)
        {
            var request = new StaticFileRequest(this, context);
            
            if (!await request.Handle())
            {
                await Next.Invoke(context);
            }
        }
    }
}