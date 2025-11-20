namespace WzComparerR2
{
    partial class FrmSkillTooltipExport
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
            this.lblSelectJobIntro = new DevComponents.DotNetBar.LabelX();
            this.clbJobName = new System.Windows.Forms.CheckedListBox();
            this.btnSort = new DevComponents.DotNetBar.ButtonX();
            this.btnSelectAll = new DevComponents.DotNetBar.ButtonX();
            this.btnReverseSelect = new DevComponents.DotNetBar.ButtonX();
            this.btnExport = new DevComponents.DotNetBar.ButtonX();
            this.SuspendLayout();
            // 
            // lblSelectJobIntro
            // 
            this.lblSelectJobIntro.AutoSize = true;
            // 
            // 
            // 
            this.lblSelectJobIntro.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblSelectJobIntro.Location = new System.Drawing.Point(10, 7);
            this.lblSelectJobIntro.Name = "lblSelectJobIntro";
            this.lblSelectJobIntro.Size = new System.Drawing.Size(222, 16);
            this.lblSelectJobIntro.TabIndex = 0;
            this.lblSelectJobIntro.Text = "내보낼 직업을 선택하세요.";
            // 
            // clbJobName
            // 
            this.clbJobName.CheckOnClick = true;
            this.clbJobName.FormattingEnabled = true;
            this.clbJobName.Location = new System.Drawing.Point(10, 30);
            this.clbJobName.Name = "clbJobName";
            this.clbJobName.Font = new System.Drawing.Font("굴림", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.clbJobName.Size = new System.Drawing.Size(430, 522);
            this.clbJobName.TabIndex = 1;
            // 
            // btnSort
            // 
            this.btnSort.Location = new System.Drawing.Point(359, 5);
            this.btnSort.Name = "btnSort";
            this.btnSort.Size = new System.Drawing.Size(80, 20);
            this.btnSort.TabIndex = 2;
            this.btnSort.Text = "기본 순서";
            this.btnSort.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnSort.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnSort.Click += new System.EventHandler(this.btnSort_Click);
            // 
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Location = new System.Drawing.Point(10, 545);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(80, 25);
            this.btnSelectAll.TabIndex = 3;
            this.btnSelectAll.Text = "모두 선택";
            this.btnSelectAll.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnSelectAll.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnSelectAll.Click += new System.EventHandler(this.btnSelectAll_Click);
            // 
            // btnReverseSelect
            // 
            this.btnReverseSelect.Location = new System.Drawing.Point(184, 545);
            this.btnReverseSelect.Name = "btnReverseSelect";
            this.btnReverseSelect.Size = new System.Drawing.Size(80, 25);
            this.btnReverseSelect.TabIndex = 4;
            this.btnReverseSelect.Text = "선택 반전";
            this.btnReverseSelect.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnReverseSelect.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnReverseSelect.Click += new System.EventHandler(this.btnReverseSelect_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(358, 545);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(80, 25);
            this.btnExport.TabIndex = 5;
            this.btnExport.Text = "내보내기";
            this.btnExport.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExport.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // FrmSkillTooltipExport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 580);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnReverseSelect);
            this.Controls.Add(this.btnSelectAll);
            this.Controls.Add(this.btnSort);
            this.Controls.Add(this.clbJobName);
            this.Controls.Add(this.lblSelectJobIntro);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSkillTooltipExport";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "스킬 툴팁 내보내기";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        
        private DevComponents.DotNetBar.LabelX lblSelectJobIntro;
        private System.Windows.Forms.CheckedListBox clbJobName;
        private DevComponents.DotNetBar.ButtonX btnSort;
        private DevComponents.DotNetBar.ButtonX btnSelectAll;
        private DevComponents.DotNetBar.ButtonX btnReverseSelect;
        private DevComponents.DotNetBar.ButtonX btnExport;
    }
}