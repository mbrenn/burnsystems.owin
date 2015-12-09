using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using System.Globalization;

namespace BurnSystems.Owin.StaticFiles
{
    /// <summary>
    /// Defines the owin middleware which serves the static files
    /// </summary>
    public class StaticFilesMiddleware : OwinMiddleware
    {
        private readonly StaticFilesConfiguration _configuration;

        public StaticFilesConfiguration Configuration => _configuration;

        private readonly StaticFileRequest _staticFileRequest;

        public StaticFilesMiddleware(OwinMiddleware next, StaticFilesConfiguration configuration) : base(next)
        {
            Debug.Assert(configuration != null, "_configuration != null");

            if (!Directory.Exists(configuration.Directory))
            {
                throw new InvalidOperationException("Path for static file directory does not exist: " +
                                                    configuration.Directory);
            }

            _configuration = configuration;
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
                await this.Next.Invoke(context);
            }
        }
    }
}