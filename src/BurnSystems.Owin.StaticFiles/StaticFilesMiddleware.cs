using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace BurnSystems.Owin.StaticFiles
{
    /// <summary>
    /// Defines the owin middleware which serves the static files
    /// </summary>
    public class StaticFilesMiddleware : OwinMiddleware
    {
        private readonly StaticFilesConfiguration _configuration;

        public StaticFilesMiddleware(OwinMiddleware next, StaticFilesConfiguration configuration) : base(next)
        {
            Debug.Assert(configuration != null, "_configuration != null");

            if (!Directory.Exists(configuration.Directory))
            {
                throw new InvalidOperationException("Path for static file directory does not exist: " + configuration.Directory);
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
            string uriPath;
            var absolutePath = DetermineAbsolutePath(context, out uriPath);

            var response = context.Response;
            if (!(await CheckIfFileIsSafeAndExisting(uriPath, absolutePath, response)))
            {
                await this.Next.Invoke(context);
            }

            await WriteFileToResponse(response, absolutePath);

        }

        private string DetermineAbsolutePath(IOwinContext context, out string uriPath)
        {
            // Gets the path of the file, which is requested by the user
            uriPath = context.Request.Uri.AbsolutePath;

            if (uriPath.StartsWith("/"))
            {
                uriPath = uriPath.Substring(1);
            }

            if (string.IsNullOrEmpty(uriPath))
            {
                uriPath = this._configuration.IndexFile;
            }

            var absolutePath = Path.Combine(this._configuration.Directory, uriPath);
            return absolutePath;
        }

        private async Task<bool> CheckIfFileIsSafeAndExisting(string uriPath, string absolutePath, IOwinResponse response)
        {
            if (Path.IsPathRooted(uriPath) || uriPath.Contains("..") || !absolutePath.StartsWith(this._configuration.Directory))
            {
                response.StatusCode = 404;
                await response.WriteAsync("Not found");
                return false;
            }

            // Checks, if the path is existing
            if (!File.Exists(absolutePath))
            {
                response.StatusCode = 404;
                await response.WriteAsync("Not found");
                return false;
            }

            return true;
        }

        private async Task WriteFileToResponse(IOwinResponse response, string absolutePath)
        {
            // Now, do the writing
            var streamSize = this._configuration.BlockWriteSize;
            var bytes = new byte[streamSize];
            using (var fileStream = File.OpenRead(absolutePath))
            {
                var token = new CancellationToken();

                int read;

                do
                {
                    read = await fileStream.ReadAsync(bytes, 0, streamSize, token);
                    if (token.IsCancellationRequested)
                    {
                        // Cancellation was requested
                        return;
                    }

                    await response.WriteAsync(bytes, 0, read, token);

                } while (read > 0);
            }
        }
    }
}