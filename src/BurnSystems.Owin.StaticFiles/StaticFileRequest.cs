using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace BurnSystems.Owin.StaticFiles
{
    public class StaticFileRequest
    {
        /// <summary>
        /// Stores the reference to the middleware
        /// </summary>
        private readonly StaticFilesMiddleware _staticFilesMiddleware;

        private readonly IOwinRequest _request;
        private readonly IOwinResponse _response;
        private string _absolutePath;
        private string _uriPath;

        public StaticFileRequest(StaticFilesMiddleware staticFilesMiddleware, IOwinContext context)
        {
            _staticFilesMiddleware = staticFilesMiddleware;
            _request = context.Request;
            _response = context.Response;
        }

        public async Task<bool> Handle()
        {
            DetermineAbsolutePath();

            if (!(CheckIfFileIsSafeAndExisting()))
            {
                return false;
            }

            await WriteFileToResponse();
            return true;
        }

        private void DetermineAbsolutePath()
        {
            // Gets the path of the file, which is requested by the user
            _uriPath = _request.Uri.AbsolutePath;

            if (_uriPath.StartsWith("/"))
            {
                _uriPath = _uriPath.Substring(1);
            }

            if (string.IsNullOrEmpty(_uriPath))
            {
                _uriPath = _staticFilesMiddleware.Configuration.IndexFile;
            }

            _absolutePath = Path.Combine(_staticFilesMiddleware.Configuration.Directory, _uriPath);
        }

        private bool CheckIfFileIsSafeAndExisting()
        {
            if (Path.IsPathRooted(_uriPath) || _uriPath.Contains("..") || !_absolutePath.StartsWith(_staticFilesMiddleware.Configuration.Directory))
            {
                SendStatusCodeAsync(404);
                return false;
            }

            // Checks, if the path is existing
            if (!File.Exists(_absolutePath))
            {
                SendStatusCodeAsync(404);
                return false;
            }

            return true;
        }

        private async Task WriteFileToResponse()
        {
            var fileInfo = new FileInfo(_absolutePath);
            var length = fileInfo.Length;

            var last = fileInfo.LastWriteTime;
            // Truncate to the second.
            var lastModified = new DateTime(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Kind);
            var lastModifiedString = lastModified.ToString(Constants.HttpDateFormat, CultureInfo.InvariantCulture);

            var etagHash = lastModified.ToFileTimeUtc() ^ length;
            var etag = Convert.ToString(etagHash, 16);
            var etagQuoted = '\"' + etag + '\"';

            var browserCache = new StaticFileBrowserCache(_request, etag, lastModified);
            {
                browserCache.ComprehendRequestHeaders();

                var preconditionState = browserCache.GetPreconditionState();
                switch (preconditionState)
                {
                    case StaticFileBrowserCache.PreconditionState.Unspecified:
                        goto case StaticFileBrowserCache.PreconditionState.ShouldProcess;

                    case StaticFileBrowserCache.PreconditionState.ShouldProcess:
                        _response.Headers.Set(Constants.LastModified, lastModifiedString);
                        _response.ETag = etagQuoted;

                        await SendFileAsync(_absolutePath);
                        return;

                    case StaticFileBrowserCache.PreconditionState.NotModified:

                        SendStatusCodeAsync(Constants.Status304NotModified);
                        return;

                    case StaticFileBrowserCache.PreconditionState.PreconditionFailed:

                        SendStatusCodeAsync(Constants.Status412PreconditionFailed);
                        return;

                    default:
                        throw new NotImplementedException(preconditionState.ToString());
                }
            }
        }

        private void SendStatusCodeAsync(int code)
        {
            _response.StatusCode = code;
        }

        private async Task SendFileAsync(string absolutePath)
        {
            // Now, do the writing
            var streamSize = _staticFilesMiddleware.Configuration.BlockWriteSize;
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

                    await _response.WriteAsync(bytes, 0, read, token);

                } while (read > 0);
            }
        }
    }
}