using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeedingFrenzyExplorer
{
    public static class Functions
    {
        public static T RawDataToObject<T>(byte[] rawData) where T : struct
        {
            var pinnedRawData = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                // Get the address of the data array
                var pinnedRawDataPtr = pinnedRawData.AddrOfPinnedObject();

                // overlay the data type on top of the raw data
                return (T)Marshal.PtrToStructure(pinnedRawDataPtr, typeof(T));
            }
            finally
            {
                // must explicitly release
                pinnedRawData.Free();
            }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        }
        public static void getArray(byte[] source, int index, byte[] destination)
        {
            //Array.Resize(ref destination, destination.Length); 
            Array.Copy(source, index, destination, 0, destination.Length);
        }

        public static void PopulateTreeView(TreeView treeView, string[] paths, char pathSeparator)
        {
            TreeNode lastNode = null;
            string subPathAgg;
            foreach (string path in paths)
            {
                subPathAgg = string.Empty;
                foreach (string subPath in path.Split(pathSeparator))
                {
                    subPathAgg += subPath + pathSeparator;
                    TreeNode[] nodes = treeView.Nodes.Find(subPathAgg, true);
                    if (nodes.Length == 0)
                        if (lastNode == null)
                            lastNode = treeView.Nodes.Add(subPathAgg, subPath);
                        else
                            lastNode = lastNode.Nodes.Add(subPathAgg, subPath);
                    else
                        lastNode = nodes[0];
                }
                lastNode = null; // This is the place code was changed

            }
        }
        public static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
        }
        public static byte[] INT32ToLittleEndian(Int32 data)
        {
            byte[] b = new byte[4];
            b[0] = (byte)data;
            b[1] = (byte)(((uint)data >> 8) & 0xFF);
            b[2] = (byte)(((uint)data >> 16) & 0xFF);
            b[3] = (byte)(((uint)data >> 24) & 0xFF);
            return b;
        }
        public static byte[] INT16ToLittleEndian(Int16 data)
        {
            byte[] b = new byte[2];
            b[0] = (byte)data;
            b[1] = (byte)(((uint)data >> 8) & 0xFF);
            return b;
        }
        public static byte[] GetMD5checksum(byte[] inputData)
        {

            //convert byte array to stream
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            stream.Write(inputData, 0, inputData.Length);

            //important: get back to start of stream
            stream.Seek(0, System.IO.SeekOrigin.Begin);

            //get a string value for the MD5 hash.
            using (var md5Instance = System.Security.Cryptography.MD5.Create())
            {
                var hashResult = md5Instance.ComputeHash(stream);

                return hashResult;
            }
        }
    }
}
