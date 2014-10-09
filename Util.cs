using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Outliner
{
    static class Util
    {
        public delegate void TreeNodeMapFun(TreeNode N);

        public static void EachNode(TreeNodeCollection Ns, TreeNodeMapFun F)
        {
            foreach (TreeNode N in Ns)
            {
                F(N);
                EachNode(N.Nodes, F);
            }
        }

        public static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; ++i)
                if (a[i] != b[i])
                    return false;
            return true;
        }
    }
}
