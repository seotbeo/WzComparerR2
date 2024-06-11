namespace WzComparerR2
{
    partial class FrmOptions
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
            this.panelEx1 = new DevComponents.DotNetBar.PanelEx();
            this.txtAPIkey = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.buttonX2 = new DevComponents.DotNetBar.ButtonX();
            this.buttonX1 = new DevComponents.DotNetBar.ButtonX();
            this.superTabControl1 = new DevComponents.DotNetBar.SuperTabControl();
            this.superTabControlPanel1 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.superTabControlPanel2 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.cmbWzVersionVerifyMode = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.chkImgCheckDisabled = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.chkWzSortByImgID = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.chkAutoCheckExtFiles = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.cmbWzEncoding = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.chkWzAutoSort = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.superTabItem1 = new DevComponents.DotNetBar.SuperTabItem();
            this.superTabItem2 = new DevComponents.DotNetBar.SuperTabItem();
            this.panelEx1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).BeginInit();
            this.superTabControl1.SuspendLayout();
            this.superTabControlPanel1.SuspendLayout();
            this.superTabControlPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelEx1
            // 
            this.panelEx1.CanvasColor = System.Drawing.SystemColors.Control;
            this.panelEx1.ColorSchemeStyle = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.panelEx1.Controls.Add(this.buttonX2);
            this.panelEx1.Controls.Add(this.buttonX1);
            this.panelEx1.DisabledBackColor = System.Drawing.Color.Empty;
            this.panelEx1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelEx1.Location = new System.Drawing.Point(0, 171);
            this.panelEx1.Name = "panelEx1";
            this.panelEx1.Size = new System.Drawing.Size(400, 30);
            this.panelEx1.Style.Alignment = System.Drawing.StringAlignment.Center;
            this.panelEx1.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
            this.panelEx1.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
            this.panelEx1.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
            this.panelEx1.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
            this.panelEx1.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
            this.panelEx1.Style.GradientAngle = 90;
            this.panelEx1.TabIndex = 0;
            // 
            // buttonX2
            // 
            this.buttonX2.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonX2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonX2.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonX2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonX2.Location = new System.Drawing.Point(331, 4);
            this.buttonX2.Name = "buttonX2";
            this.buttonX2.Size = new System.Drawing.Size(65, 23);
            this.buttonX2.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonX2.TabIndex = 1;
            this.buttonX2.Text = "キャンセル";
            // 
            // buttonX1
            // 
            this.buttonX1.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonX1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonX1.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonX1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonX1.Location = new System.Drawing.Point(264, 4);
            this.buttonX1.Name = "buttonX1";
            this.buttonX1.Size = new System.Drawing.Size(60, 23);
            this.buttonX1.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonX1.TabIndex = 0;
            this.buttonX1.Text = "OK";
            // 
            // superTabControl1
            // 
            this.superTabControl1.AutoCloseTabs = false;
            // 
            // 
            // 
            // 
            // 
            // 
            this.superTabControl1.ControlBox.CloseBox.Name = "";
            // 
            // 
            // 
            this.superTabControl1.ControlBox.MenuBox.Name = "";
            this.superTabControl1.ControlBox.Name = "";
            this.superTabControl1.ControlBox.SubItems.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabControl1.ControlBox.MenuBox,
            this.superTabControl1.ControlBox.CloseBox});
            this.superTabControl1.Controls.Add(this.superTabControlPanel1);
            this.superTabControl1.Controls.Add(this.superTabControlPanel2);
            this.superTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControl1.Location = new System.Drawing.Point(0, 0);
            this.superTabControl1.Name = "superTabControl1";
            this.superTabControl1.ReorderTabsEnabled = true;
            this.superTabControl1.SelectedTabFont = new System.Drawing.Font("MS Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.superTabControl1.SelectedTabIndex = 0;
            this.superTabControl1.Size = new System.Drawing.Size(400, 171);
            this.superTabControl1.TabAlignment = DevComponents.DotNetBar.eTabStripAlignment.Left;
            this.superTabControl1.TabFont = new System.Drawing.Font("MS Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.superTabControl1.TabIndex = 4;
            this.superTabControl1.Tabs.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabItem1,
            this.superTabItem2});
            this.superTabControl1.Text = "superTabControl1";
            // 
            // superTabControlPanel1
            // 
            this.superTabControlPanel1.Controls.Add(this.cmbWzVersionVerifyMode);
            this.superTabControlPanel1.Controls.Add(this.labelX2);
            this.superTabControlPanel1.Controls.Add(this.chkImgCheckDisabled);
            this.superTabControlPanel1.Controls.Add(this.chkWzSortByImgID);
            this.superTabControlPanel1.Controls.Add(this.chkAutoCheckExtFiles);
            this.superTabControlPanel1.Controls.Add(this.cmbWzEncoding);
            this.superTabControlPanel1.Controls.Add(this.labelX1);
            this.superTabControlPanel1.Controls.Add(this.chkWzAutoSort);
            this.superTabControlPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel1.Location = new System.Drawing.Point(49, 0);
            this.superTabControlPanel1.Name = "superTabControlPanel1";
            this.superTabControlPanel1.Size = new System.Drawing.Size(351, 171);
            this.superTabControlPanel1.TabIndex = 1;
            this.superTabControlPanel1.TabItem = this.superTabItem1;
            // 
            // superTabControlPanel2
            // 
            this.superTabControlPanel2.Controls.Add(this.labelX3);
            this.superTabControlPanel2.Controls.Add(this.txtAPIkey);
            this.superTabControlPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel2.Location = new System.Drawing.Point(49, 0);
            this.superTabControlPanel2.Name = "superTabControlPanel2";
            this.superTabControlPanel2.Size = new System.Drawing.Size(351, 171);
            this.superTabControlPanel2.TabIndex = 1;
            this.superTabControlPanel2.TabItem = this.superTabItem2;
            this.superTabControlPanel2.Visible = false;
            // 
            // cmbWzVersionVerifyMode
            // 
            this.cmbWzVersionVerifyMode.DisplayMember = "Text";
            this.cmbWzVersionVerifyMode.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbWzVersionVerifyMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWzVersionVerifyMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbWzVersionVerifyMode.FormattingEnabled = true;
            this.cmbWzVersionVerifyMode.ItemHeight = 13;
            this.cmbWzVersionVerifyMode.Location = new System.Drawing.Point(148, 132);
            this.cmbWzVersionVerifyMode.Name = "cmbWzVersionVerifyMode";
            this.cmbWzVersionVerifyMode.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cmbWzVersionVerifyMode.TabIndex = 8;
            // 
            // labelX2
            // 
            this.labelX2.AutoSize = true;
            this.labelX2.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Location = new System.Drawing.Point(14, 134);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(142, 16);
            this.labelX2.TabIndex = 7;
            this.labelX2.Text = "WZのバージョン確認方法";
            // 
            // labelX3
            // 
            this.labelX3.AutoSize = true;
            this.labelX3.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(14, 15);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(142, 16);
            this.labelX3.TabIndex = 9;
            this.labelX3.Text = "APIキー";
            // 
            // txtAPIkey
            // 
            this.txtAPIkey.Border.Class = "TextBoxBorder";
            this.txtAPIkey.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtAPIkey.Location = new System.Drawing.Point(63, 13);
            this.txtAPIkey.Name = "txtAPIkey";
            this.txtAPIkey.Size = new System.Drawing.Size(220, 21);
            this.txtAPIkey.TabIndex = 10;
            // 
            // chkImgCheckDisabled
            // 
            this.chkImgCheckDisabled.AutoSize = true;
            this.chkImgCheckDisabled.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkImgCheckDisabled.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkImgCheckDisabled.Location = new System.Drawing.Point(14, 110);
            this.chkImgCheckDisabled.Name = "chkImgCheckDisabled";
            this.chkImgCheckDisabled.Size = new System.Drawing.Size(212, 16);
            this.chkImgCheckDisabled.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkImgCheckDisabled.TabIndex = 6;
            this.chkImgCheckDisabled.Text = "IMGチェックサム検証をスキップする";
            // 
            // chkWzSortByImgID
            // 
            this.chkWzSortByImgID.AutoSize = true;
            this.chkWzSortByImgID.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkWzSortByImgID.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkWzSortByImgID.Location = new System.Drawing.Point(31, 36);
            this.chkWzSortByImgID.Name = "chkWzSortByImgID";
            this.chkWzSortByImgID.Size = new System.Drawing.Size(180, 16);
            this.chkWzSortByImgID.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkWzSortByImgID.TabIndex = 5;
            this.chkWzSortByImgID.Text = "IMGファイルごとに並べ替える";
            // 
            // chkAutoCheckExtFiles
            // 
            this.chkAutoCheckExtFiles.AutoSize = true;
            this.chkAutoCheckExtFiles.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkAutoCheckExtFiles.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkAutoCheckExtFiles.Location = new System.Drawing.Point(14, 86);
            this.chkAutoCheckExtFiles.Name = "chkAutoCheckExtFiles";
            this.chkAutoCheckExtFiles.Size = new System.Drawing.Size(225, 16);
            this.chkAutoCheckExtFiles.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkAutoCheckExtFiles.TabIndex = 4;
            this.chkAutoCheckExtFiles.Text = "分割されたWZファイルを自動検出する";
            // 
            // cmbWzEncoding
            // 
            this.cmbWzEncoding.DisplayMember = "Text";
            this.cmbWzEncoding.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbWzEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWzEncoding.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbWzEncoding.FormattingEnabled = true;
            this.cmbWzEncoding.ItemHeight = 13;
            this.cmbWzEncoding.Location = new System.Drawing.Point(148, 59);
            this.cmbWzEncoding.Name = "cmbWzEncoding";
            this.cmbWzEncoding.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cmbWzEncoding.TabIndex = 3;
            // 
            // labelX1
            // 
            this.labelX1.AutoSize = true;
            this.labelX1.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(14, 61);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(84, 16);
            this.labelX1.TabIndex = 2;
            this.labelX1.Text = "WZ符号化方式";
            // 
            // chkWzAutoSort
            // 
            this.chkWzAutoSort.AutoSize = true;
            this.chkWzAutoSort.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.chkWzAutoSort.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkWzAutoSort.Location = new System.Drawing.Point(14, 15);
            this.chkWzAutoSort.Name = "chkWzAutoSort";
            this.chkWzAutoSort.Size = new System.Drawing.Size(170, 16);
            this.chkWzAutoSort.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkWzAutoSort.TabIndex = 0;
            this.chkWzAutoSort.Text = "WZファイルの自動並べ替え";
            // 
            // superTabItem1
            // 
            this.superTabItem1.AttachedControl = this.superTabControlPanel1;
            this.superTabItem1.GlobalItem = false;
            this.superTabItem1.Name = "superTabItem1";
            this.superTabItem1.Text = "一般";
            // 
            // superTabItem2
            // 
            this.superTabItem2.AttachedControl = this.superTabControlPanel2;
            this.superTabItem2.GlobalItem = false;
            this.superTabItem2.Name = "superTabItem2";
            this.superTabItem2.Text = "NX OpenAPI";
            // 
            // FrmOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 201);
            this.Controls.Add(this.superTabControl1);
            this.Controls.Add(this.panelEx1);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("MS PGothic", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "設定";
            this.panelEx1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).EndInit();
            this.superTabControl1.ResumeLayout(false);
            this.superTabControlPanel1.ResumeLayout(false);
            this.superTabControlPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.PanelEx panelEx1;
        private DevComponents.DotNetBar.Controls.TextBoxX txtAPIkey;
        private DevComponents.DotNetBar.SuperTabControl superTabControl1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel2;
        private DevComponents.DotNetBar.SuperTabItem superTabItem1;
        private DevComponents.DotNetBar.SuperTabItem superTabItem2;
        private DevComponents.DotNetBar.ButtonX buttonX2;
        private DevComponents.DotNetBar.ButtonX buttonX1;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkWzAutoSort;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cmbWzEncoding;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkAutoCheckExtFiles;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkWzSortByImgID;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkImgCheckDisabled;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cmbWzVersionVerifyMode;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.LabelX labelX3;
    }
}