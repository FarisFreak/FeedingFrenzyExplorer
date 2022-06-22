using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeedingFrenzyExplorer.Struct
{
    public struct CSifFileUnique
    {
        public Int32 Id;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] UniqueId;      // 0x0008
        public Int32 FileCount;
    }
}
