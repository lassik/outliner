using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outliner
{
    static class FileFormat
    {
        bool byteArraysEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; ++i)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        byte[] magic = { 0xff, 0x00, 0x1a, 0x0d, 0x0a, 0x0a, 0x0d };

        string unisig = "4c356fea@net.lassikortela.treefile";

        string path = "treedata";

        Stream file;

        void wbyte(int x)
        {
            file.WriteByte((byte)x);
        }

        void wint(int x)
        {
            while (x > 0x7f)
            {
                wbyte(0x80 | (x & 0x7f));
                x >>= 7;
            }
            wbyte(x);
        }

        void wrawbytes(byte[] x)
        {
            file.Write(x, 0, x.Length);
        }

        void wcntbytes(byte[] x)
        {
            wint(x.Length);
            wrawbytes(x);
        }

        void wcntstring(string x)
        {
            wcntbytes(new UTF8Encoding().GetBytes(x));
        }

        void wunisig()
        {
            wrawbytes(magic);
            wcntstring(unisig);
        }

        void wnode(TreeNode x)
        {
            wcntstring(x.Text);
            wint(x.Nodes.Count);
            foreach (TreeNode tn in x.Nodes)
            {
                wnode(tn);
            }
        }

        void write()
        {
            using (file = new FileStream(path, FileMode.Create))
            {
                wunisig();
                wnode(tv.Nodes[0]);
            }
        }

        int rbyte()
        {
            int by = file.ReadByte();
            if (by == -1)
            {
                throw new Exception("Premature end of file 1");
            }
            return by;
        }

        int rint()
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

        byte[] rrawbytes(int n)
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

        string rcntstring()
        {
            return new UTF8Encoding().GetString(rrawbytes(rint()));
        }

        void runisig()
        {
            if (!byteArraysEqual(magic, rrawbytes(magic.Length)))
            {
                throw new Exception("Bad magic");
            }
            if (rcntstring() != unisig)
            {
                throw new Exception("Bad signature");
            }
        }

        TreeNode rnode()
        {
            TreeNode tn = new TreeNode(rcntstring());
            for (int n = rint(); n > 0; --n)
            {
                tn.Nodes.Add(rnode());
            }
            return tn;
        }

        void read()
        {
            file = new FileStream(path, FileMode.Open);
            try
            {
                runisig();
                TreeNode newRoot = rnode();
                tv.Nodes.Clear();
                tv.Nodes.Add(newRoot);
            }
            finally
            {
                file.Close();
            }
        }



    }
}
