namespace CleanRepo
{
    internal class Redirect
    {
        public string? source_path { get; set; } = null;
        public string? source_path_from_root { get; set; } = null;
        public string? source_path_absolute { get; set; } = null;
        public string redirect_url { get; set; }
        public bool? redirect_document_id { get; set; } = null;
    }
}
