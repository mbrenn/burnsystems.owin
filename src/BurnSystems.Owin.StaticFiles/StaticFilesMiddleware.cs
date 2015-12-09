using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace BurnSystems.Owin.StaticFiles
{
    public class StaticFilesMiddleware : OwinMiddleware
    {
        private StaticFileConfiguration configuration;

        public StaticFilesMiddleware(OwinMiddleware next, StaticFileConfiguration configuration) : base(next)
        {
            Debug.Assert(configuration != null, "configuration != null");

            if (!Directory.Exists(configuration.Directory))
            {
                throw new InvalidOperationException("Path for static file directory does not exist: " + configuration.Directory);
            }

            this.configuration = configuration;
        }

        public override async Task Invoke(IOwinContext context)
        {
            // Gets the path of the file, which is requested by the user
            var uriPath = context.Request.Uri.AbsolutePath;

            if (uriPath.StartsWith("/"))
            {
                uriPath = uriPath.Substring(1);
            }

            if (string.IsNullOrEmpty(uriPath))
            {
                uriPath = this.configuration.IndexFile;
            }

            var response = context.Response;
            var absolutePath = Path.Combine(this.configuration.Directory, uriPath);

            if (!(await CheckIfFileIsSafeAndExisting(uriPath, absolutePath, response)))
            {
                await this.Next.Invoke(context);
            }

            await WriteFileToResponse(response, absolutePath);

        }

        private async Task<bool> CheckIfFileIsSafeAndExisting(string uriPath, string absolutePath, IOwinResponse response)
        {
            if (Path.IsPathRooted(uriPath) || uriPath.Contains("..") || !absolutePath.StartsWith(this.configuration.Directory))
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
            var streamSize = this.configuration.BlockWriteSize;
            var bytes = new byte[streamSize];
            using (var fileStream = File.OpenRead(absolutePath))
            {
                var token = new CancellationToken();

                var read = await fileStream.ReadAsync(bytes, 0, streamSize, token);
                if (read > 0)
                {
                    await response.WriteAsync(bytes, 0, read, token);
                }
            }
        }
    }
}