namespace BurnSystems.Owin.StaticFiles
{
    public static class Constants
    {
        internal const string IfMatch = "If-Match";
        internal const string IfNoneMatch = "If-None-Match";
        internal const string IfModifiedSince = "If-Modified-Since";
        internal const string IfUnmodifiedSince = "If-Unmodified-Since";
        internal const string LastModified = "Last-Modified";

        internal const string HttpDateFormat = "r";

        internal const int Status304NotModified = 304;
        internal const int Status412PreconditionFailed = 412;
    }
}
