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
        private static readonly byte[] SignatureMagicNumber = { 0xff, 0x00, 0x1a, 0x0d, 0x0a, 0x0a, 0x0d };

        private const string SignatureFormatName = "4c356fea@net.lassikortela.treefile";

        public class Writer : IDisposable
        {
            private Stream file;

            private void WriteRawByte(int byt)
            {
                file.WriteByte((byte)byt);
            }

            private void WriteNonnegativeInt(int value)
            {
                while (value > 0x7f)
                {
                    WriteRawByte(0x80 | (value & 0x7f));
                    value >>= 7;
                }
                WriteRawByte(value);
            }

            private void WriteRawBytes(byte[] bytes)
            {
                file.Write(bytes, 0, bytes.Length);
            }

            private void WriteCountedBytes(byte[] bytes)
            {
                WriteNonnegativeInt(bytes.Length);
                WriteRawBytes(bytes);
            }

            private void WriteCountedString(string str)
            {
                WriteCountedBytes(new UTF8Encoding().GetBytes(str));
            }

            private void WriteFileFormatSignature()
            {
                WriteRawBytes(SignatureMagicNumber);
                WriteCountedString(SignatureFormatName);
            }

            private void WriteTreeNode(TreeNode node)
            {
                WriteCountedString(node.Text);
                WriteNonnegativeInt(node.Nodes.Count);
                foreach (TreeNode subnode in node.Nodes)
                {
                    WriteTreeNode(subnode);
                }
            }

            public Writer(string filename)
            {
                this.file = new FileStream(filename, FileMode.Create);
            }

            public void WriteTree(TreeNode root)
            {
                WriteFileFormatSignature();
                WriteTreeNode(root);
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

            private int ReadRawByte()
            {
                var byt = file.ReadByte();
                if (byt == -1)
                {
                    throw new Exception("Corrupt file: File ends too soon");
                }
                return byt;
            }

            private int ReadNonnegativeInt()
            {
                var value = 0;
                var shift = 0;
                while (true)
                {
                    var byt = ReadRawByte();
                    value |= (byt & 0x7f) << shift;
                    shift += 7;
                    if (0 == (byt & 0x80)) break;
                }
                return value;
            }

            private byte[] ReadRawBytes(int n)
            {
                var bytes = new byte[n];
                var ndone = 0;
                var nleft = n;
                while (nleft > 0)
                {
                    n = file.Read(bytes, ndone, nleft);
                    if (n == 0)
                    {
                        throw new Exception("Corrupt file: File ends too soon");
                    }
                    ndone += n;
                    nleft -= n;
                }
                return bytes;
            }

            private byte[] ReadCountedBytes()
            {
                return ReadRawBytes(ReadNonnegativeInt());
            }

            private string ReadCountedString()
            {
                return new UTF8Encoding().GetString(ReadCountedBytes());
            }

            private void ReadFileFormatSignature()
            {
                var valid =
                    Util.ByteArraysEqual(SignatureMagicNumber,
                        ReadRawBytes(SignatureMagicNumber.Length)) &&
                    (ReadCountedString() == SignatureFormatName);
                if (!valid)
                {
                    throw new Exception("Corrupt file: Incorrect file format signature");
                }
            }

            private TreeNode ReadTreeNode()
            {
                var node = new TreeNode(ReadCountedString());
                var n = ReadNonnegativeInt();
                while (n > 0)
                {
                    node.Nodes.Add(ReadTreeNode());
                    n--;
                }
                return node;
            }

            public Reader(string filename)
            {
                this.file = new FileStream(filename, FileMode.Open);
            }

            public TreeNode ReadTree()
            {
                ReadFileFormatSignature();
                return ReadTreeNode();
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
