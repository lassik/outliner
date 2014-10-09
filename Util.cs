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
    }
}
