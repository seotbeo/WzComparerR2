using System.Drawing;

namespace WzComparerR2
{
    partial class FrmWorldArchiveBrowser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cmbRegion = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.cmbType = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.advTreeLife = new DevComponents.AdvTree.AdvTree();
            this.nodeConnector1 = new DevComponents.AdvTree.NodeConnector();
            this.elementStyle1 = new DevComponents.DotNetBar.ElementStyle();
            this.advTreeMap = new DevComponents.AdvTree.AdvTree();
            this.picWorldArchiveImg = new System.Windows.Forms.PictureBox();
            this.richDescription = new DevComponents.DotNetBar.Controls.RichTextBoxEx();
            this.btnExport = new DevComponents.DotNetBar.ButtonX();
            this.btnLocateExtraIllust = new DevComponents.DotNetBar.ButtonX();
            ((System.ComponentModel.ISupportInitialize)(this.advTreeLife)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.advTreeMap)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picWorldArchiveImg)).BeginInit();
            this.SuspendLayout();
            // 
            // btnExport
            // 
            this.btnExport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExport.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExport.Location = new System.Drawing.Point(605, 736);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(155, 23);
            this.btnExport.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExport.TabIndex = 9;
            this.btnExport.Text = "내보내기";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            this.btnExport.Enabled = false;
            // 
            // btnLocateExtraIllust
            // 
            this.btnLocateExtraIllust.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnLocateExtraIllust.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnLocateExtraIllust.Location = new System.Drawing.Point(440, 736);
            this.btnLocateExtraIllust.Name = "btnLocateExtraIllust";
            this.btnLocateExtraIllust.Size = new System.Drawing.Size(155, 23);
            this.btnLocateExtraIllust.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnLocateExtraIllust.TabIndex = 8;
            this.btnLocateExtraIllust.Text = "특별 조사 기록 찾아가기";
            this.btnLocateExtraIllust.Click += new System.EventHandler(this.btnLocateExtraIllust_Click);
            this.btnLocateExtraIllust.Enabled = false;
            // 
            // cmbRegion
            // 
            this.cmbRegion.DisplayMember = "Text";
            this.cmbRegion.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRegion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbRegion.FormattingEnabled = true;
            this.cmbRegion.ItemHeight = 13;
            this.cmbRegion.Location = new System.Drawing.Point(9, 14);
            this.cmbRegion.Name = "cmbRegion";
            this.cmbRegion.Size = new System.Drawing.Size(202, 19);
            this.cmbRegion.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cmbRegion.TabIndex = 0;
            this.cmbRegion.WatermarkColor = System.Drawing.Color.Gray;
            this.cmbRegion.WatermarkText = "지역";
            this.cmbRegion.SelectedValueChanged += new System.EventHandler(this.cmbRegion_SelectedValueChanged);
            // 
            // cmbType
            // 
            this.cmbType.DisplayMember = "Text";
            this.cmbType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbType.FormattingEnabled = true;
            this.cmbType.ItemHeight = 13;
            this.cmbType.Location = new System.Drawing.Point(221, 14);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(202, 19);
            this.cmbType.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cmbType.TabIndex = 2;
            this.cmbType.WatermarkColor = System.Drawing.Color.Gray;
            this.cmbType.WatermarkText = "유형";
            this.cmbType.SelectedValueChanged += new System.EventHandler(this.cmbType_SelectedValueChanged);
            // 
            // advTreeLife
            // 
            this.advTreeLife.AccessibleRole = System.Windows.Forms.AccessibleRole.Outline;
            this.advTreeLife.AllowDrop = true;
            this.advTreeLife.BackColor = System.Drawing.SystemColors.Window;
            // 
            // 
            // 
            this.advTreeLife.BackgroundStyle.Class = "TreeBorderKey";
            this.advTreeLife.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.advTreeLife.DoubleClickTogglesNode = false;
            this.advTreeLife.DragDropEnabled = false;
            this.advTreeLife.DragDropNodeCopyEnabled = false;
            this.advTreeLife.HideSelection = true;
            this.advTreeLife.Location = new System.Drawing.Point(221, 44);
            this.advTreeLife.Name = "advTreeLife";
            this.advTreeLife.NodesConnector = this.nodeConnector1;
            this.advTreeLife.NodeStyle = this.elementStyle1;
            this.advTreeLife.PathSeparator = ";";
            this.advTreeLife.Size = new System.Drawing.Size(202, 716);
            this.advTreeLife.TabIndex = 3;
            this.advTreeLife.Text = "advTreeLife";
            this.advTreeLife.AfterNodeSelect += new DevComponents.AdvTree.AdvTreeNodeEventHandler(this.advTreeLife_AfterNodeSelect);
            this.advTreeLife.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.advTreeLife_MouseDoubleClick);
            // 
            // elementStyle1
            // 
            this.elementStyle1.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.elementStyle1.Name = "elementStyle1";
            this.elementStyle1.TextColor = System.Drawing.SystemColors.ControlText;
            // 
            // advTreeMap
            // 
            this.advTreeMap.AccessibleRole = System.Windows.Forms.AccessibleRole.Outline;
            this.advTreeMap.AllowDrop = true;
            this.advTreeMap.BackColor = System.Drawing.SystemColors.Window;
            // 
            // 
            // 
            this.advTreeMap.BackgroundStyle.Class = "TreeBorderKey";
            this.advTreeMap.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.advTreeMap.DoubleClickTogglesNode = false;
            this.advTreeMap.DragDropEnabled = false;
            this.advTreeMap.DragDropNodeCopyEnabled = false;
            this.advTreeMap.ExpandWidth = 4;
            this.advTreeMap.HideSelection = true;
            this.advTreeMap.Location = new System.Drawing.Point(9, 44);
            this.advTreeMap.Name = "advTreeMap";
            this.advTreeMap.NodeStyle = this.elementStyle1;
            this.advTreeMap.PathSeparator = ";";
            this.advTreeMap.Size = new System.Drawing.Size(202, 716);
            this.advTreeMap.Styles.Add(this.elementStyle1);
            this.advTreeMap.TabIndex = 1;
            this.advTreeMap.Text = "advTreeMap";
            this.advTreeMap.AfterNodeSelect += new DevComponents.AdvTree.AdvTreeNodeEventHandler(this.advTreeMap_AfterNodeSelect);
            // 
            // picWorldArchiveImg
            // 
            this.picWorldArchiveImg.Location = new System.Drawing.Point(440, 17);
            this.picWorldArchiveImg.Name = "picWorldArchiveImg";
            this.picWorldArchiveImg.Size = new System.Drawing.Size(320, 320);
            this.picWorldArchiveImg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picWorldArchiveImg.TabIndex = 4;
            this.picWorldArchiveImg.TabStop = false;
            // 
            // richDescription
            // 
            this.richDescription.BackgroundStyle.Class = "RichTextBoxBorder";
            this.richDescription.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.richDescription.Location = new System.Drawing.Point(440, 343);
            this.richDescription.Name = "richDescription";
            this.richDescription.Rtf = "{\\rtf1\\ansi\\ansicpg936\\deff0\\deflang1033\\deflangfe1042{\\fonttbl{\\f0\\fnil\\fcharset" +
    "129 \\\'b5\\\'b8\\\'bf\\\'f2;}}\r\n\\viewkind4\\uc1\\pard\\lang1042\\f0\\fs18\\par\r\n}\r\n";
            this.richDescription.Size = new System.Drawing.Size(320, 383);
            this.richDescription.TabIndex = 5;
            this.richDescription.ReadOnly = true;
            // 
            // FrmWorldArchiveBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(770, 770);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnLocateExtraIllust);
            this.Controls.Add(this.richDescription);
            this.Controls.Add(this.picWorldArchiveImg);
            this.Controls.Add(this.advTreeLife);
            this.Controls.Add(this.cmbType);
            this.Controls.Add(this.advTreeMap);
            this.Controls.Add(this.cmbRegion);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Dotum", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmWorldArchiveBrowser";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Text = "월드 아카이브";
            ((System.ComponentModel.ISupportInitialize)(this.advTreeLife)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.advTreeMap)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picWorldArchiveImg)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
        private DevComponents.DotNetBar.Controls.ComboBoxEx cmbRegion;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cmbType;
        private DevComponents.AdvTree.AdvTree advTreeLife;
        private DevComponents.AdvTree.AdvTree advTreeMap;
        private DevComponents.AdvTree.NodeConnector nodeConnector1;
        private DevComponents.DotNetBar.ElementStyle elementStyle1;
        private System.Windows.Forms.PictureBox picWorldArchiveImg;
        private DevComponents.DotNetBar.Controls.RichTextBoxEx richDescription;
        private DevComponents.DotNetBar.ButtonX btnExport;
        private DevComponents.DotNetBar.ButtonX btnLocateExtraIllust;
    }
}