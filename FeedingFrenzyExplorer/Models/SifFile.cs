using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeedingFrenzyExplorer.Models
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct SifFile
    {
        public int Offset;
        public int Size;
        public List<byte> _Dummy;
        public string Path;
        public string FileName;
        public bool Marked;
        public List<byte> Binary;
        public List<byte> _oriBinary;
    }
}
