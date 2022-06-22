using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeedingFrenzyExplorer.Struct
{
    public struct CSifFile
    {
        public Int32 Offset;        // 0x0000
        public Int32 Size;          // 0x0004
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] _sDummy;      // 0x0008
        public Int16 NameSize;      // 0x0018
    }
}
