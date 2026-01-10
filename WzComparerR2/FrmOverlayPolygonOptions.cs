using DevComponents.AdvTree;
using DevComponents.DotNetBar.Controls;
using DevComponents.Editors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.Config;
using WzComparerR2.Controls;

namespace WzComparerR2
{
    public partial class FrmOverlayPolygonOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlayPolygonOptions(List<Point> vertices)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.txtX.Value = 0;
            this.txtY.Value = 0;
            this.buttonAdd.Click += ButtonAdd_Click;
            this.buttonRemove.Click += ButtonRemove_Click;
            this.buttonClear.Click += ButtonClear_Click;
            this.Vertices = vertices;
            this.LoadAdvTree();
        }

        public List<Point> Vertices { get; set; }

        public void LoadAdvTree()
        {
            this.advTree1.Nodes.Clear();
            var i = 0;
            foreach (var vertex in Vertices)
            {
                Node node = new Node($"정점 {i++}: ({vertex.X}, {vertex.Y})");
                this.advTree1.Nodes.Add(node);
            }
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            Vertices.Add(new Point(this.txtX.Value, this.txtY.Value));
            LoadAdvTree();
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            if (this.advTree1.SelectedNode != null)
            {
                int index = this.advTree1.SelectedNode.Index;
                if (index >= 0 && index < Vertices.Count)
                {
                    Vertices.RemoveAt(index);
                }
            }
            LoadAdvTree();
        }

        private void ButtonClear_Click(object sender, EventArgs e)
        {
            Vertices.Clear();
            LoadAdvTree();
        }
    }
}