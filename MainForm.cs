using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Forms;

namespace Outliner
{
    public partial class MainForm : Form
    {
        private const string filename = "treedata";

        EventHandler cmdHandler(Cmd cmd) 
        {
            return delegate(object sender, System.EventArgs e) 
            { 
                cmd(); 
            }; 
        }

        void addToMenu(MenuItem menu) 
        {
            menu.MenuItems.Add("-"); 
        }

        void addToMenu(MenuItem menu, string text, Cmd cmd, Shortcut key) 
        {
            menu.MenuItems.Add(new MenuItem(text, cmdHandler(cmd), key)); 
        }

        void cmdNoop()
        {
        }

        void cmdRevert() 
        {
            TreeNode newRoot;
            using (var reader = new FileFormat.Reader(filename))
            {
                 newRoot = reader.ReadTree();
            }
            tv.Nodes.Clear();
            tv.Nodes.Add(newRoot);
        }

        void cmdSave()
        {
            TreeNode root = tv.Nodes[0];
            using (var writer = new FileFormat.Writer(filename))
            {
                writer.WriteTree(root);
            }
            CalendarOps.DumpCalendarAsHTML(tv.Nodes, filename + "cal.html");
        }

        TreeNode ClipboardNode;

        void cmdCut() 
        {
            if (tv.SelectedNode == null) return;  //xxx
            ClipboardNode = tv.SelectedNode;
            tv.SelectedNode.Remove(); 
        }

        void cmdPasteSiblingsAfter() 
        {
            if (ClipboardNode == null) 
            { 
                //xxx
                MessageBox.Show("Nothing in the clipboard");
                return;  
            } 
            InsertNodeUnder(tv.SelectedNode.Parent, tv.SelectedNode.Index + 1, ClipboardNode);
            ClipboardNode = null; 
        }

        void cmdDelete() 
        {
            //if (EditingLabel) 
            tv.SelectedNode.Remove(); 
        }

        void cmdEditText() 
        {
            tv.SelectedNode.BeginEdit(); 
        }

        void cmdToggleSorted()
        {
        }    

        class NodeAsText 
        {
            private StringBuilder sb = new StringBuilder();
            private void Rec(TreeNode node, int depth) 
            {
                for (int i = 0; i < depth * 2; i++) sb.Append(" ");
                sb.Append("* ");
                sb.Append(node.Text);
                sb.Append("\n");
                foreach (TreeNode subnode in node.Nodes)
                    Rec(subnode, depth + 1); 
            }
            public string Result {
                get
                { 
                    return sb.ToString(); 
                } 
            }
            public NodeAsText(TreeNode node) {
                Rec(node, 0);
            }
        }

        void cmdCopyAsText() 
        {
            Clipboard.SetText(new NodeAsText(tv.SelectedNode).Result); 
        }

        void cmdSearch() 
        {
            if (!tb.Visible) {
                tb.Text = "";
                tb.Visible = true; 
            }
            tb.Select();
        }

        void CountNodes(TreeNode node, ref int nodes, ref int leaves) 
        {
            nodes++;
            if (node.Nodes.Count == 0)
                leaves++;
            else
                foreach (TreeNode subnode in node.Nodes)
                    CountNodes(subnode, ref nodes, ref leaves); 
        }

        void cmdCount() 
        {
            int nodes = 0; int leaves = 0;
            CountNodes(tv.SelectedNode, ref nodes, ref leaves);
            MessageBox.Show(String.Format("{0} nodes, {1} leaves", nodes, leaves)); 
        }

        void cmdInsertSiblingBefore() 
        {
            InsertNodeUnder(tv.SelectedNode.Parent, tv.SelectedNode.Index); 
        }

        void cmdInsertSiblingAfter() 
        {
            InsertNodeUnder(tv.SelectedNode.Parent, tv.SelectedNode.Index + 1); 
        }

        void cmdInsertChildLast() 
        {
            InsertNodeUnder(tv.SelectedNode, tv.SelectedNode.Nodes.Count); 
        }

        bool fileSaved = false;
        bool fileModified = false;

        delegate void Cmd();
        TreeView tv;
        TextBox tb;

        class NodeData
        {
            bool Sorted = false; 
        }

        string newNodeText = "<New node>";

        TreeNode InsertNodeUnder(TreeNode Parent, int i, TreeNode Node)
        {
            if (Parent == null) return null;
            Parent.Nodes.Insert(i, Node);
            tv.SelectedNode = Parent.Nodes[i];
            return Node; 
        }

        TreeNode InsertNodeUnder(TreeNode Parent, int i)
        {
            TreeNode Node = InsertNodeUnder(Parent, i, new TreeNode(newNodeText));
            if (Node != null) Node.BeginEdit();
            return Node; 
        }

        bool match(TreeNode node) 
        {
            return (node == null) || (node.Text.ToUpper().IndexOf(tb.Text.ToUpper()) != -1);
        }

        TreeNode SearchBackward(TreeNode node) 
        {
            for (;;)
            {
                if (node.PrevNode != null)
                {
                    node = node.PrevNode;
                    while (node.LastNode != null) node = node.LastNode; 
                }
                else
                    node = node.Parent;
                if (match(node))
                    return node;
            }
        }

        TreeNode SearchForward(TreeNode node) 
        {
            for (;;)
            {
                if (node.FirstNode != null)
                    node = node.FirstNode;
                else if (node.NextNode != null)
                    node = node.NextNode;
                else
                {
                    for (;;)
                    {
                        node = node.Parent;
                        if (node == null) break;
                        if (node.NextNode != null)
                        {
                            node = node.NextNode;
                            break; 
                        } 
                    } 
                }
                if (match(node))
                    return node;
            }
        }

        delegate TreeNode SearchFun(TreeNode Node);

        void DoSearch(SearchFun SF) 
        {
            if (tv.SelectedNode == null) tv.SelectedNode = tv.Nodes[0];
            if (tb.Text == "") return;
            TreeNode tn = SF(tv.SelectedNode);
            if (tn != null) tv.SelectedNode = tn; 
        }

        void cmdSearchForward() 
        {
            DoSearch(SearchForward); 
        }

        void cmdSearchBackward() 
        {
            DoSearch(SearchBackward); 
        }

        bool EditingLabel = false;

        void tvBeforeLabelEdit(object sender, NodeLabelEditEventArgs e) 
        {
            EditingLabel = true; 
        }

        void tvAfterLabelEdit(object sender, NodeLabelEditEventArgs e) 
        {
            EditingLabel = false; 
        }
    
        void OnClosing(Object sender, CancelEventArgs e)
        {
        }

        public MainForm()
        {
            InitializeComponent();
            Closing += OnClosing;
            tv = new TreeView();
            tv.BeforeLabelEdit += tvBeforeLabelEdit;
            tv.AfterLabelEdit += tvAfterLabelEdit;
            tv.Dock = DockStyle.Fill;
            tv.LabelEdit = true;
            tv.HideSelection = false;
            tv.Nodes.Add("Root");
            tb = new TextBox();
            tb.Dock = DockStyle.Bottom;
            tb.TextChanged += cmdHandler(cmdSearchForward);
            tb.KeyDown += delegate(object sender, KeyEventArgs e)
                {
                    switch (e.KeyCode)
                    {
                    case Keys.Return:
                    {
                        if (e.Shift)
                        {
                            cmdSearchBackward();
                        }
                        else
                        {
                            cmdSearchForward();
                        }
                        break;
                    }
                    case Keys.Escape:
                    {
                        tb.Visible = false;
                        break;
                    }
                    };
                };
            tb.Visible = false;

            Controls.Add(tv);
            Controls.Add(tb);
            Text = "Tree";
            Menu = new MainMenu();

            MenuItem menuFile;
            Menu.MenuItems.Add(menuFile = new MenuItem("File"));
            addToMenu(menuFile, "Revert", cmdRevert, Shortcut.None);
            addToMenu(menuFile, "Save", cmdSave, Shortcut.CtrlS);
            addToMenu(menuFile);
            addToMenu(menuFile, "Exit", cmdNoop, Shortcut.None);

            MenuItem menuEdit;
            Menu.MenuItems.Add(menuEdit = new MenuItem("Edit"));
            addToMenu(menuEdit, "Cut", cmdCut, Shortcut.None);
            addToMenu(menuEdit, "Copy", cmdCopyAsText, Shortcut.None);
            addToMenu(menuEdit, "Paste siblings before", cmdNoop, Shortcut.None);
            addToMenu(menuEdit, "Paste siblings after", cmdPasteSiblingsAfter, Shortcut.None);
            addToMenu(menuEdit, "Paste children last", cmdNoop, Shortcut.None);
            addToMenu(menuEdit, "Delete", cmdDelete, Shortcut.None);
            addToMenu(menuEdit);
            addToMenu(menuEdit, "Edit text", cmdEditText, Shortcut.F2);
            addToMenu(menuEdit, "Toggle sorted", cmdToggleSorted, Shortcut.None);
            addToMenu(menuEdit);
            addToMenu(menuEdit, "Move Up", cmdToggleSorted, Shortcut.None);
            addToMenu(menuEdit, "Move Down", cmdToggleSorted, Shortcut.None);
            addToMenu(menuEdit, "Move Left", cmdToggleSorted, Shortcut.None);
            addToMenu(menuEdit, "Move Right", cmdToggleSorted, Shortcut.None);
            addToMenu(menuEdit);
            addToMenu(menuEdit, "Next Siblings Move Left", cmdToggleSorted, Shortcut.None);
            addToMenu(menuEdit, "All Children Move Left", cmdToggleSorted, Shortcut.None);

            MenuItem menuInsert;
            Menu.MenuItems.Add(menuInsert = new MenuItem("Insert"));
            addToMenu(menuInsert, "Sibling before", cmdInsertSiblingBefore, Shortcut.None);
            addToMenu(menuInsert, "Sibling after", cmdInsertSiblingAfter, Shortcut.None);
            addToMenu(menuInsert, "Child last", cmdInsertChildLast, Shortcut.None);

            MenuItem menuView;
            Menu.MenuItems.Add(menuView = new MenuItem("View"));
            addToMenu(menuView, "Find", cmdSearch, Shortcut.CtrlF);
            addToMenu(menuView, "Count", cmdCount, Shortcut.None);
            addToMenu(menuView);
            addToMenu(menuView, "Expand One Level", cmdNoop, Shortcut.None);
            addToMenu(menuView, "Expand All Levels", cmdNoop, Shortcut.None);
            addToMenu(menuView, "Collapse One Level", cmdNoop, Shortcut.None);
            addToMenu(menuView, "Collapse All Levels", cmdNoop, Shortcut.None);

            cmdRevert();
        }

    }
}
