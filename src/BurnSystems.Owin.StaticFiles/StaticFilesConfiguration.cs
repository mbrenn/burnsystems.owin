namespace BurnSystems.Owin.StaticFiles
{
    public class StaticFilesConfiguration
    {
        /// <summary>
        /// Gets or sets the directory which is used to show the files
        /// </summary>
        public string Directory { get; set; }

        public string IndexFile { get; set; } = "index.html";

        public int BlockWriteSize { get; set; } = 65536;
    }
}