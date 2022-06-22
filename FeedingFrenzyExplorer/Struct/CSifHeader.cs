using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeedingFrenzyExplorer.Struct
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CSifHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Header;       // 0x0000
        public Int32 Dummy;         // 0x0004
        public Int32 Offset;        // 0x0008
    } // Size 0x000C
}
