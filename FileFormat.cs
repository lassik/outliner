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

        private static Stream file;

        private static void wbyte(int x)
        {
            file.WriteByte((byte)x);
        }

        private static void wint(int x)
        {
            while (x > 0x7f)
            {
                wbyte(0x80 | (x & 0x7f));
                x >>= 7;
            }
            wbyte(x);
        }

        private static void wrawbytes(byte[] x)
        {
            file.Write(x, 0, x.Length);
        }

        private static void wcntbytes(byte[] x)
        {
            wint(x.Length);
            wrawbytes(x);
        }

        private static void wcntstring(string x)
        {
            wcntbytes(new UTF8Encoding().GetBytes(x));
        }

        private static void wunisig()
        {
            wrawbytes(magic);
            wcntstring(unisig);
        }

        private static void wnode(TreeNode x)
        {
            wcntstring(x.Text);
            wint(x.Nodes.Count);
            foreach (TreeNode tn in x.Nodes)
            {
                wnode(tn);
            }
        }

        private static void write()
        {
            using (file = new FileStream(path, FileMode.Create))
            {
                wunisig();
                wnode(tv.Nodes[0]);
            }
        }

        private static int rbyte()
        {
            int by = file.ReadByte();
            if (by == -1)
            {
                throw new Exception("Premature end of file 1");
            }
            return by;
        }

        private static int rint()
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

        private static byte[] rrawbytes(int n)
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

        private static string rcntstring()
        {
            return new UTF8Encoding().GetString(rrawbytes(rint()));
        }

        private static void runisig()
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

        private static TreeNode rnode()
        {
            TreeNode tn = new TreeNode(rcntstring());
            for (int n = rint(); n > 0; --n)
            {
                tn.Nodes.Add(rnode());
            }
            return tn;
        }

        public static TreeNode ReadTreeFromFile(string filename)
        {
            using (var file = new FileStream(filename, FileMode.Open))
            {
                runisig();
                TreeNode newRoot = rnode();
            }
        }
    }
}
