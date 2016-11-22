using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X3Snapshot
{
    public class FileHash
    {
        public enum MatchStatus
        {
            NoMatch,
            ExactMatch,
            Modified
        }

        public string RelativeFileName { get; set; }
        public string ASCIIHash { get; set; }
        public MatchStatus CurrentStatus { get; set; }

        public FileHash(string relativeFileName, string asciiHash)
        {
            RelativeFileName = relativeFileName;
            ASCIIHash = asciiHash;
        }
        
        // 0 = no match
        // 1 = exact match
        // 2 = match BUT modified file
        public MatchStatus FileHashMatch(FileHash fileHash)
        {
            if (!fileHash.RelativeFileName.Equals(this.RelativeFileName)) return MatchStatus.NoMatch;
            if (fileHash.ASCIIHash.Equals(this.ASCIIHash)) return MatchStatus.ExactMatch;
            return MatchStatus.Modified;
        }
    }
}
