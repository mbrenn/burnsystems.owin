using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Owin;

namespace BurnSystems.Owin.StaticFiles
{
    public class StaticFileBrowserCache
    {
        internal enum PreconditionState
        {
            Unspecified,
            NotModified,
            ShouldProcess,
            PreconditionFailed,
        }

        private readonly string _etagOfFile;
        private readonly DateTime _lastModifiedOfFile;
        private readonly IOwinRequest _request;

        private PreconditionState _ifMatchState;
        private PreconditionState _ifNoneMatchState;
        private PreconditionState _ifUnmodifiedSinceState;
        private PreconditionState _ifModifiedSinceState;

        public StaticFileBrowserCache(IOwinRequest request, string etag, DateTime lastModifiedOfFile)
        {
            _request = request;
            _etagOfFile = etag;
            _lastModifiedOfFile = lastModifiedOfFile;
        }

        public void ComprehendRequestHeaders()
        {
            ComputeIfMatch();

            ComputeIfModifiedSince();
        }

        private void ComputeIfMatch()
        {
            // 14.24 If-Match
            IList<string> ifMatch = _request.Headers.GetCommaSeparatedValues(Constants.IfMatch); // Removes quotes
            if (ifMatch != null)
            {
                _ifMatchState = PreconditionState.PreconditionFailed;
                foreach (var segment in ifMatch)
                {
                    if (segment.Equals("*", StringComparison.Ordinal)
                        || segment.Equals(_etagOfFile, StringComparison.Ordinal))
                    {
                        _ifMatchState = PreconditionState.ShouldProcess;
                        break;
                    }
                }
            }

            // 14.26 If-None-Match
            IList<string> ifNoneMatch = _request.Headers.GetCommaSeparatedValues(Constants.IfNoneMatch);
            if (ifNoneMatch != null)
            {
                _ifNoneMatchState = PreconditionState.ShouldProcess;
                foreach (var segment in ifNoneMatch)
                {
                    if (segment.Equals("*", StringComparison.Ordinal)
                        || segment.Equals(_etagOfFile, StringComparison.Ordinal))
                    {
                        _ifNoneMatchState = PreconditionState.NotModified;
                        break;
                    }
                }
            }
        }

        private void ComputeIfModifiedSince()
        {
            // 14.25 If-Modified-Since
            string ifModifiedSinceString = _request.Headers.Get(Constants.IfModifiedSince);
            DateTime ifModifiedSince;
            if (TryParseHttpDate(ifModifiedSinceString, out ifModifiedSince))
            {
                bool modified = ifModifiedSince < _lastModifiedOfFile;
                _ifModifiedSinceState = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
            }

            // 14.28 If-Unmodified-Since
            string ifUnmodifiedSinceString = _request.Headers.Get(Constants.IfUnmodifiedSince);
            DateTime ifUnmodifiedSince;
            if (TryParseHttpDate(ifUnmodifiedSinceString, out ifUnmodifiedSince))
            {
                bool unmodified = ifUnmodifiedSince >= _lastModifiedOfFile;
                _ifUnmodifiedSinceState = unmodified ? PreconditionState.ShouldProcess : PreconditionState.PreconditionFailed;
            }
        }
        
        public static bool TryParseHttpDate(string dateString, out DateTime parsedDate)
        {
            return DateTime.TryParseExact(dateString, Constants.HttpDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
        }

        internal PreconditionState GetPreconditionState()
        {
            return GetMaxPreconditionState(_ifMatchState, _ifNoneMatchState,
                _ifModifiedSinceState, _ifUnmodifiedSinceState);
        }

        private static PreconditionState GetMaxPreconditionState(params PreconditionState[] states)
        {
            PreconditionState max = PreconditionState.Unspecified;
            foreach ( var state in states)
            {
                if (state > max)
                {
                    max = state;
                }
            }

            return max;
        }
    }

}
