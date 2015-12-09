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

            var request = context.Request;
            var response = context.Response;
            if (!(CheckIfFileIsSafeAndExisting(uriPath, absolutePath, response)))
            {
                await this.Next.Invoke(context);
            }

            await WriteFileToResponse(request, response, absolutePath);
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

        private bool CheckIfFileIsSafeAndExisting(string uriPath, string absolutePath, IOwinResponse response)
        {
            if (Path.IsPathRooted(uriPath) || uriPath.Contains("..") || !absolutePath.StartsWith(this._configuration.Directory))
            {
                SendStatusCodeAsync(response, 404);
                return false;
            }

            // Checks, if the path is existing
            if (!File.Exists(absolutePath))
            {
                SendStatusCodeAsync(response, 404);
                return false;
            }

            return true;
        }

        private static void SendStatusCodeAsync(IOwinResponse response, int code)
        {
            response.StatusCode = code;
        }

        private async Task WriteFileToResponse(IOwinRequest request, IOwinResponse response, string absolutePath)
        {
            var fileInfo = new FileInfo(absolutePath);
            var length = fileInfo.Length;

            var last = fileInfo.LastWriteTime;
            // Truncate to the second.
            var lastModified = new DateTime(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Kind);
            var lastModifiedString = lastModified.ToString(Constants.HttpDateFormat, CultureInfo.InvariantCulture);

            var etagHash = lastModified.ToFileTimeUtc() ^ length;
            var etag = Convert.ToString(etagHash, 16);
            var etagQuoted = '\"' + etag + '\"';

            var browserCache = new StaticFileBrowserCache(request, etag, lastModified);
            {
                browserCache.ComprehendRequestHeaders();

                var preconditionState = browserCache.GetPreconditionState();
                switch (preconditionState)
                {
                    case StaticFileBrowserCache.PreconditionState.Unspecified:
                        goto case StaticFileBrowserCache.PreconditionState.ShouldProcess;

                    case StaticFileBrowserCache.PreconditionState.ShouldProcess:
                        response.Headers.Set(Constants.LastModified, lastModifiedString);
                        response.ETag = etagQuoted;

                        await WriteFileAsync(response, absolutePath);
                        return;

                    case StaticFileBrowserCache.PreconditionState.NotModified:

                        SendStatusCodeAsync(response, Constants.Status304NotModified);
                        return;

                    case StaticFileBrowserCache.PreconditionState.PreconditionFailed:

                        SendStatusCodeAsync(response, Constants.Status412PreconditionFailed);
                        return;

                    default:
                        throw new NotImplementedException(preconditionState.ToString());
                }
            }
        }

        private async Task WriteFileAsync(IOwinResponse response, string absolutePath)
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