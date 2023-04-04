using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocateProjects
{
    internal struct DiscoveryResult
    {
        public const int RETURN_GOOD = 0;
        public const int RETURN_NOPROJ = 1;
        public const int RETURN_TOOMANY = 2;
        public const int RETURN_SLN = 3;

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
