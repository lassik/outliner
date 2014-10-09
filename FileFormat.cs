using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Outliner
{
    static class FileFormat
    {
        private static byte[] magic = { 0xff, 0x00, 0x1a, 0x0d, 0x0a, 0x0a, 0x0d };

        private const string unisig = "4c356fea@net.lassikortela.treefile";

        public class Writer : IDisposable
        {
            private Stream file;

            private void wbyte(int x)
            {
                file.WriteByte((byte)x);
            }

            private void wint(int x)
            {
                while (x > 0x7f)
                {
                    wbyte(0x80 | (x & 0x7f));
                    x >>= 7;
                }
                wbyte(x);
            }

            private void wrawbytes(byte[] x)
            {
                file.Write(x, 0, x.Length);
            }

            private void wcntbytes(byte[] x)
            {
                wint(x.Length);
                wrawbytes(x);
            }

            private void wcntstring(string x)
            {
                wcntbytes(new UTF8Encoding().GetBytes(x));
            }

            private void wunisig()
            {
                wrawbytes(magic);
                wcntstring(unisig);
            }

            private void wnode(TreeNode x)
            {
                wcntstring(x.Text);
                wint(x.Nodes.Count);
                foreach (TreeNode tn in x.Nodes)
                {
                    wnode(tn);
                }
            }

            public Writer(string filename)
            {
                this.file = new FileStream(filename, FileMode.Create);
            }

            public void WriteTree(TreeNode root)
            {
                wunisig();
                wnode(root);
            }

            public void Dispose()
            {
                if (this.file != null)
                {
                    this.file.Dispose();
                    this.file = null;
                }
            }
        }

        public class Reader : IDisposable
        {
            private Stream file;

            private int rbyte()
            {
                int by = file.ReadByte();
                if (by == -1)
                {
                    throw new Exception("Premature end of file 1");
                }
                return by;
            }

            private int rint()
            {
                int x = 0; int sh = 0; int by;
                for (; ; )
                {
                    by = rbyte();
                    x |= (by & 0x7f) << sh;
                    sh += 7;
                    if (0 == (by & 0x80)) return x;
                }
            }

            private byte[] rrawbytes(int n)
            {
                byte[] bytes = new byte[n];
                int nleft = n; int nread = 0;
                while (nleft > 0)
                {
                    n = file.Read(bytes, nread, nleft);
                    if (n == 0)
                    {
                        throw new Exception("Premature end of file n");
                    }
                    nread += n; nleft -= n;
                }
                return bytes;
            }

            private string rcntstring()
            {
                return new UTF8Encoding().GetString(rrawbytes(rint()));
            }

            private void runisig()
            {
                if (!Util.ByteArraysEqual(magic, rrawbytes(magic.Length)))
                {
                    throw new Exception("Bad magic");
                }
                if (rcntstring() != unisig)
                {
                    throw new Exception("Bad signature");
                }
            }

            private TreeNode rnode()
            {
                TreeNode tn = new TreeNode(rcntstring());
                for (int n = rint(); n > 0; --n)
                {
                    tn.Nodes.Add(rnode());
                }
                return tn;
            }

            public Reader(string filename)
            {
                this.file = new FileStream(filename, FileMode.Open);
            }

            public TreeNode ReadTree()
            {
                runisig();
                return rnode();
            }

            public void Dispose()
            {
                if (this.file != null)
                {
                    this.file.Dispose();
                    this.file = null;
                }
            }
        }
    }
}
