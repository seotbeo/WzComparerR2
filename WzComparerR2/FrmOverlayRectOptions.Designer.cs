using DevComponents.DotNetBar.Controls;

namespace WzComparerR2
{
    partial class FrmOverlayRectOptions // base code from FrmGifClipOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmOverlayRectOptions));
            this.buttonOK = new DevComponents.DotNetBar.ButtonX();
            this.buttonCancel = new DevComponents.DotNetBar.ButtonX();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.labelX8 = new DevComponents.DotNetBar.LabelX();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.labelX6 = new DevComponents.DotNetBar.LabelX();
            this.labelX9 = new DevComponents.DotNetBar.LabelX();
            this.labelX10 = new DevComponents.DotNetBar.LabelX();
            this.labelX7 = new DevComponents.DotNetBar.LabelX();
            this.txtLeft = new DevComponents.Editors.IntegerInput();
            this.txtRight = new DevComponents.Editors.IntegerInput();
            this.txtTop = new DevComponents.Editors.IntegerInput();
            this.txtBottom = new DevComponents.Editors.IntegerInput();
            this.txtRadius = new DevComponents.Editors.IntegerInput();
            this.txtStart = new DevComponents.Editors.IntegerInput();
            this.txtEnd = new DevComponents.Editors.IntegerInput();
            this.txtSpeedX = new DevComponents.Editors.IntegerInput();
            this.txtSpeedY = new DevComponents.Editors.IntegerInput();
            this.txtGoX = new DevComponents.Editors.IntegerInput();
            this.txtGoY = new DevComponents.Editors.IntegerInput();
            this.txtAlpha = new DevComponents.Editors.IntegerInput();
            this.colorPickerButton1 = new DevComponents.DotNetBar.ColorPickerButton();
            this.chkIsCircle = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRadius)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtEnd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpeedX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpeedY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtGoX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtGoY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtAlpha)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonOK.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(70, 3);
            this.buttonOK.Margin = new System.Windows.Forms.Padding(35, 3, 4, 3);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(88, 23);
            this.buttonOK.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonOK.Symbol = "";
            this.buttonOK.SymbolSize = 1F;
            this.buttonOK.TabIndex = 14;
            this.buttonOK.Text = "OK";
            // 
            // buttonCancel
            // 
            this.buttonCancel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonCancel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(237, 3);
            this.buttonCancel.Margin = new System.Windows.Forms.Padding(4, 3, 35, 3);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(88, 23);
            this.buttonCancel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonCancel.TabIndex = 15;
            this.buttonCancel.Text = "Cancel";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 63F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Controls.Add(this.labelX1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelX3, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelX4, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelX8, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelX5, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelX6, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.labelX9, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.labelX10, 1, 6);
            this.tableLayoutPanel1.Controls.Add(this.labelX7, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.txtLeft, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtRight, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtTop, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtBottom, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtRadius, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.txtStart, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.txtEnd, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.txtSpeedX, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.txtSpeedY, 3, 5);
            this.tableLayoutPanel1.Controls.Add(this.txtGoX, 2, 6);
            this.tableLayoutPanel1.Controls.Add(this.txtGoY, 3, 6);
            this.tableLayoutPanel1.Controls.Add(this.txtAlpha, 3, 7);
            this.tableLayoutPanel1.Controls.Add(this.colorPickerButton1, 2, 7);
            this.tableLayoutPanel1.Controls.Add(this.chkIsCircle, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(9, 8);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 8;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(396, 235);
            this.tableLayoutPanel1.TabIndex = 11;
            // 
            // labelX1
            // 
            this.labelX1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(4, 3);
            this.labelX1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(55, 23);
            this.labelX1.TabIndex = 11;
            this.labelX1.Text = "설정";
            // 
            // labelX3
            // 
            this.labelX3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(67, 3);
            this.labelX3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(134, 23);
            this.labelX3.TabIndex = 11;
            this.labelX3.Text = "LT";
            // 
            // labelX4
            // 
            this.labelX4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX4.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX4.Location = new System.Drawing.Point(67, 32);
            this.labelX4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(134, 23);
            this.labelX4.TabIndex = 11;
            this.labelX4.Text = "RB";
            // 
            // labelX8
            // 
            this.labelX8.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX8.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX8.Location = new System.Drawing.Point(67, 61);
            this.labelX8.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX8.Name = "labelX8";
            this.labelX8.Size = new System.Drawing.Size(134, 23);
            this.labelX8.TabIndex = 11;
            this.labelX8.Text = "반지름";
            // 
            // labelX5
            // 
            this.labelX5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX5.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX5.Location = new System.Drawing.Point(67, 90);
            this.labelX5.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(134, 23);
            this.labelX5.TabIndex = 11;
            this.labelX5.Text = "시작 딜레이 (ms)";
            // 
            // labelX6
            // 
            this.labelX6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX6.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX6.Location = new System.Drawing.Point(67, 119);
            this.labelX6.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX6.Name = "labelX7";
            this.labelX6.Size = new System.Drawing.Size(134, 23);
            this.labelX6.TabIndex = 11;
            this.labelX6.Text = "종료 딜레이 (ms)";
            // 
            // labelX9
            // 
            this.labelX9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX9.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX9.Location = new System.Drawing.Point(67, 148);
            this.labelX9.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX9.Name = "labelX9";
            this.labelX9.Size = new System.Drawing.Size(134, 23);
            this.labelX9.TabIndex = 12;
            this.labelX9.Text = "X Y 이동속도 (px/초)";
            // 
            // labelX10
            // 
            this.labelX10.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX10.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX10.Location = new System.Drawing.Point(67, 177);
            this.labelX10.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX10.Name = "labelX10";
            this.labelX10.Size = new System.Drawing.Size(134, 23);
            this.labelX10.TabIndex = 12;
            this.labelX10.Text = "X Y 이동거리 (px)";
            // 
            // labelX7
            // 
            this.labelX7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX7.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX7.Location = new System.Drawing.Point(67, 206);
            this.labelX7.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelX7.Name = "labelX7";
            this.labelX7.Size = new System.Drawing.Size(134, 26);
            this.labelX7.TabIndex = 11;
            this.labelX7.Text = "색상/투명도";
            // 
            // txtLeft
            // 
            this.txtLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtLeft.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtLeft.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtLeft.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtLeft.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtLeft.Location = new System.Drawing.Point(209, 3);
            this.txtLeft.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtLeft.MaxValue = 8192;
            this.txtLeft.MinValue = -8192;
            this.txtLeft.Name = "txtLeft";
            this.txtLeft.ShowUpDown = true;
            this.txtLeft.Size = new System.Drawing.Size(87, 21);
            this.txtLeft.TabIndex = 0;
            // 
            // txtRight
            // 
            this.txtRight.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtRight.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtRight.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtRight.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtRight.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtRight.Location = new System.Drawing.Point(209, 32);
            this.txtRight.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtRight.MaxValue = 8192;
            this.txtRight.MinValue = -8192;
            this.txtRight.Name = "txtRight";
            this.txtRight.ShowUpDown = true;
            this.txtRight.Size = new System.Drawing.Size(87, 21);
            this.txtRight.TabIndex = 2;
            // 
            // txtTop
            // 
            this.txtTop.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtTop.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtTop.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtTop.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtTop.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtTop.Location = new System.Drawing.Point(304, 3);
            this.txtTop.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtTop.MaxValue = 8192;
            this.txtTop.MinValue = -8192;
            this.txtTop.Name = "txtTop";
            this.txtTop.ShowUpDown = true;
            this.txtTop.Size = new System.Drawing.Size(88, 21);
            this.txtTop.TabIndex = 1;
            // 
            // txtBottom
            // 
            this.txtBottom.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtBottom.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtBottom.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtBottom.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtBottom.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtBottom.Location = new System.Drawing.Point(304, 32);
            this.txtBottom.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtBottom.MaxValue = 8192;
            this.txtBottom.MinValue = -8192;
            this.txtBottom.Name = "txtBottom";
            this.txtBottom.ShowUpDown = true;
            this.txtBottom.Size = new System.Drawing.Size(88, 21);
            this.txtBottom.TabIndex = 3;
            // 
            // txtRadius
            // 
            this.txtRadius.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtRadius.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtRadius.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtRadius.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.tableLayoutPanel1.SetColumnSpan(this.txtRadius, 2);
            this.txtRadius.Enabled = false;
            this.txtRadius.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtRadius.Location = new System.Drawing.Point(209, 61);
            this.txtRadius.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtRadius.MaxValue = 65530;
            this.txtRadius.MinValue = 0;
            this.txtRadius.Name = "txtRadius";
            this.txtRadius.ShowUpDown = true;
            this.txtRadius.Size = new System.Drawing.Size(183, 21);
            this.txtRadius.TabIndex = 4;
            // 
            // txtStart
            // 
            this.txtStart.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtStart.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtStart.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtStart.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.tableLayoutPanel1.SetColumnSpan(this.txtStart, 2);
            this.txtStart.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtStart.Increment = 10;
            this.txtStart.Location = new System.Drawing.Point(209, 90);
            this.txtStart.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtStart.MaxValue = 65530;
            this.txtStart.MinValue = 0;
            this.txtStart.Name = "txtBottom";
            this.txtStart.ShowUpDown = true;
            this.txtStart.Size = new System.Drawing.Size(183, 21);
            this.txtStart.TabIndex = 5;
            // 
            // txtEnd
            // 
            this.txtEnd.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtEnd.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtEnd.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtEnd.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.tableLayoutPanel1.SetColumnSpan(this.txtEnd, 2);
            this.txtEnd.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtEnd.Increment = 10;
            this.txtEnd.Location = new System.Drawing.Point(209, 119);
            this.txtEnd.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtEnd.MaxValue = 65530;
            this.txtEnd.MinValue = 0;
            this.txtEnd.Name = "txtEnd";
            this.txtEnd.ShowUpDown = true;
            this.txtEnd.Size = new System.Drawing.Size(183, 21);
            this.txtEnd.TabIndex = 6;
            // 
            // txtSpeedX
            // 
            this.txtSpeedX.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtSpeedX.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtSpeedX.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtSpeedX.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtSpeedX.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtSpeedX.Location = new System.Drawing.Point(209, 148);
            this.txtSpeedX.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSpeedX.MaxValue = 8192;
            this.txtSpeedX.MinValue = -8192;
            this.txtSpeedX.Name = "txtSpeedX";
            this.txtSpeedX.ShowUpDown = true;
            this.txtSpeedX.Size = new System.Drawing.Size(87, 21);
            this.txtSpeedX.TabIndex = 7;
            // 
            // txtSpeedY
            // 
            this.txtSpeedY.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtSpeedY.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtSpeedY.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtSpeedY.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtSpeedY.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtSpeedY.Location = new System.Drawing.Point(304, 148);
            this.txtSpeedY.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSpeedY.MaxValue = 8192;
            this.txtSpeedY.MinValue = -8192;
            this.txtSpeedY.Name = "txtSpeedY";
            this.txtSpeedY.ShowUpDown = true;
            this.txtSpeedY.Size = new System.Drawing.Size(88, 21);
            this.txtSpeedY.TabIndex = 8;
            // 
            // txtGoX
            // 
            this.txtGoX.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtGoX.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtGoX.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtGoX.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtGoX.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtGoX.Location = new System.Drawing.Point(209, 177);
            this.txtGoX.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtGoX.MaxValue = 16384;
            this.txtGoX.MinValue = 0;
            this.txtGoX.Name = "txtGoX";
            this.txtGoX.ShowUpDown = true;
            this.txtGoX.Size = new System.Drawing.Size(87, 21);
            this.txtGoX.TabIndex = 9;
            // 
            // txtGoY
            // 
            this.txtGoY.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtGoY.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtGoY.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtGoY.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtGoY.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtGoY.Location = new System.Drawing.Point(304, 177);
            this.txtGoY.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtGoY.MaxValue = 16384;
            this.txtGoY.MinValue = 0;
            this.txtGoY.Name = "txtGoY";
            this.txtGoY.ShowUpDown = true;
            this.txtGoY.Size = new System.Drawing.Size(88, 21);
            this.txtGoY.TabIndex = 10;
            // 
            // txtAlpha
            // 
            this.txtAlpha.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtAlpha.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtAlpha.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtAlpha.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtAlpha.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtAlpha.Location = new System.Drawing.Point(304, 206);
            this.txtAlpha.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtAlpha.MaxValue = 100;
            this.txtAlpha.MinValue = 0;
            this.txtAlpha.Name = "txtAlpha";
            this.txtAlpha.ShowUpDown = true;
            this.txtAlpha.Size = new System.Drawing.Size(88, 21);
            this.txtAlpha.TabIndex = 12;
            // 
            // colorPickerButton1
            // 
            this.colorPickerButton1.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.colorPickerButton1.AutoExpandOnClick = true;
            this.colorPickerButton1.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.colorPickerButton1.Image = ((System.Drawing.Image)(resources.GetObject("colorPickerButton1.Image")));
            this.colorPickerButton1.Location = new System.Drawing.Point(208, 206);
            this.colorPickerButton1.Name = "colorPickerButton1";
            this.colorPickerButton1.SelectedColorImageRectangle = new System.Drawing.Rectangle(2, 2, 12, 12);
            this.colorPickerButton1.Size = new System.Drawing.Size(37, 17);
            this.colorPickerButton1.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.colorPickerButton1.TabIndex = 11;
            // 
            // chkIsCircle
            // 
            // 
            // 
            // 
            this.chkIsCircle.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkIsCircle.Location = new System.Drawing.Point(3, 32);
            this.chkIsCircle.Name = "chkIsCircle";
            this.chkIsCircle.Size = new System.Drawing.Size(57, 17);
            this.chkIsCircle.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkIsCircle.TabIndex = 13;
            this.chkIsCircle.Text = "원";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.buttonCancel, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonOK, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(9, 243);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(396, 30);
            this.tableLayoutPanel2.TabIndex = 11;
            // 
            // FrmOverlayRectOptions
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(414, 281);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.tableLayoutPanel2);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmOverlayRectOptions";
            this.Padding = new System.Windows.Forms.Padding(9, 8, 9, 8);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "범위 그리기 설정";
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRadius)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtEnd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpeedX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpeedY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtGoX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtGoY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtAlpha)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.ButtonX buttonOK;
        private DevComponents.DotNetBar.ButtonX buttonCancel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private DevComponents.Editors.IntegerInput txtLeft;
        private DevComponents.Editors.IntegerInput txtRight;
        private DevComponents.Editors.IntegerInput txtTop;
        private DevComponents.Editors.IntegerInput txtBottom;
        private DevComponents.Editors.IntegerInput txtStart;
        private DevComponents.Editors.IntegerInput txtEnd;
        private DevComponents.Editors.IntegerInput txtRadius;
        private DevComponents.Editors.IntegerInput txtAlpha;
        private DevComponents.Editors.IntegerInput txtSpeedX;
        private DevComponents.Editors.IntegerInput txtSpeedY;
        private DevComponents.Editors.IntegerInput txtGoX;
        private DevComponents.Editors.IntegerInput txtGoY;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.LabelX labelX4;
        private DevComponents.DotNetBar.LabelX labelX5;
        private DevComponents.DotNetBar.LabelX labelX6;
        private DevComponents.DotNetBar.LabelX labelX7;
        private DevComponents.DotNetBar.LabelX labelX8;
        private DevComponents.DotNetBar.LabelX labelX9;
        private DevComponents.DotNetBar.LabelX labelX10;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkIsCircle;
        private DevComponents.DotNetBar.ColorPickerButton colorPickerButton1;
    }
}