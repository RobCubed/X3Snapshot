using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X3Snapshot
{
    public class VersionSet
    {
        public string VersionName { get; set; }
        public List<FileHash> Files { get; set; }

        public VersionSet()
        {
            Files = new List<FileHash>();
        }
    }
}
