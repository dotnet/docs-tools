namespace Snippets5000
{
    internal struct DiscoveryResult
    {
        public const int RETURN_GOOD = 0;
        public const int RETURN_NOPROJ = 1;
        public const int RETURN_TOOMANY = 2;
        public const int RETURN_SLN_NOPROJ = 3;
        public const int RETURN_SLN_PROJ_MISSING = 4;

        public const int RETURN_TEMP_SLNFOUND = 99;

        public readonly int Code { get; init; }
        public readonly string InputFile { get; init; }
        public readonly string DiscoveredFile { get; init; }

        public DiscoveryResult(int code, string inputFile, string discoveredFile)
        {
            Code = code;
            InputFile = inputFile;
            DiscoveredFile = discoveredFile;
        }

        public override string ToString() =>
            $"{Code}|{InputFile}|{DiscoveredFile}";
    }
}
