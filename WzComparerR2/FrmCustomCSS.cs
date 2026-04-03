using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WzComparerR2.Config;

namespace WzComparerR2
{
    public partial class FrmCustomCSS : DevComponents.DotNetBar.Office2007Form
    {
        public FrmCustomCSS()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
        }

        private bool DarkMode;

        public Color backgroundColor
        {
            get { return colorPickerBackgroundColor.SelectedColor; }
            set { colorPickerBackgroundColor.SelectedColor = value; }
        }

        public Color normalTextColor
        {
            get { return colorPickerNormalTextColor.SelectedColor; }
            set { colorPickerNormalTextColor.SelectedColor = value; }
        }
        public Color changedBackgroundColor
        {
            get { return colorPickerChangedBackgroundColor.SelectedColor; }
            set { colorPickerChangedBackgroundColor.SelectedColor = value; }
        }
        public Color addedBackgroundColor
        {
            get { return colorPickerAddedBackgroundColor.SelectedColor; }
            set { colorPickerAddedBackgroundColor.SelectedColor = value; }
        }

        public Color removedBackgroundColor
        {
            get { return colorPickerRemovedBackgroundColor.SelectedColor; }
            set { colorPickerRemovedBackgroundColor.SelectedColor = value; }
        }

        public Color changedTextColor
        {
            get { return colorPickerChangedTextColor.SelectedColor; }
            set { colorPickerChangedTextColor.SelectedColor = value; }
        }

        public Color addedTextColor
        {
            get { return colorPickerAddedTextColor.SelectedColor; }
            set { colorPickerAddedTextColor.SelectedColor = value; }
        }

        public Color removedTextColor
        {
            get { return colorPickerRemovedTextColor.SelectedColor; }
            set { colorPickerRemovedTextColor.SelectedColor = value; }
        }

        public Color hyperlinkColor
        {
            get { return colorPickerHyperlinkColor.SelectedColor; }
            set { colorPickerHyperlinkColor.SelectedColor = value; }
        }

        private void btnDefault_Click(object sender, EventArgs e)
        {
            if (!DarkMode)
            {
                this.backgroundColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
                this.normalTextColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
                this.changedBackgroundColor = Color.FromArgb(Int32.Parse("ff003049", NumberStyles.HexNumber));
                this.addedBackgroundColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
                this.removedBackgroundColor = Color.FromArgb(Int32.Parse("ff462306", NumberStyles.HexNumber));
                this.changedTextColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
                this.addedTextColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
                this.removedTextColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
                this.hyperlinkColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
                this.btnDefault.Text = "라이트 모드";
            }
            else
            {
                this.backgroundColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
                this.normalTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
                this.changedBackgroundColor = Color.FromArgb(Int32.Parse("fffff4c4", NumberStyles.HexNumber));
                this.addedBackgroundColor = Color.FromArgb(Int32.Parse("ffebf2f8", NumberStyles.HexNumber));
                this.removedBackgroundColor = Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber));
                this.changedTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
                this.addedTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
                this.removedTextColor = Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber));
                this.hyperlinkColor = Color.FromArgb(Int32.Parse("ff0000ff", NumberStyles.HexNumber));
                this.btnDefault.Text = "다크 모드";
            }
            this.DarkMode = !this.DarkMode;
        }

        private void colorPickers_SelectedColorChanged(object sender, EventArgs e)
        {
            this.richTextBoxEx1.ReadOnly = false;
            this.richTextBoxEx1.Clear();
            this.richTextBoxEx1.BackColorRichTextBox = backgroundColor;
            var contents = new[]
            {
                new {Text = "하이퍼 링크" + Environment.NewLine, Fore = hyperlinkColor, Back = backgroundColor, Underline = true},
                new {Text = "기본 텍스트" + Environment.NewLine, Fore = normalTextColor, Back = backgroundColor, Underline = false},
                new {Text = "변경 내용 텍스트" + Environment.NewLine, Fore = changedTextColor, Back = changedBackgroundColor, Underline = false},
                new {Text = "추가 내용 텍스트" + Environment.NewLine, Fore = addedTextColor, Back = addedBackgroundColor, Underline = false},
                new {Text = "제거 내용 텍스트", Fore = removedTextColor, Back = removedBackgroundColor, Underline = false}
            };
            foreach (var line in contents)
            {
                int start = richTextBoxEx1.TextLength;
                richTextBoxEx1.AppendText(line.Text);
                richTextBoxEx1.Select(start, line.Text.Length);
                richTextBoxEx1.SelectionColor = line.Fore;
                richTextBoxEx1.SelectionBackColor = line.Back;
                richTextBoxEx1.SelectionAlignment = HorizontalAlignment.Center;
                if (line.Underline)
                {
                    richTextBoxEx1.SelectionFont = new Font(richTextBoxEx1.Font, FontStyle.Underline);
                }
            }
            this.richTextBoxEx1.ReadOnly = true;
        }

        public void LoadConfig(CustomCSSConfig config)
        {
            this.backgroundColor = config.BackgroundColor;
            this.normalTextColor = config.NormalTextColor;
            this.changedBackgroundColor = config.ChangedBackgroundColor;
            this.addedBackgroundColor = config.AddedBackgroundColor;
            this.removedBackgroundColor = config.RemovedBackgroundColor;
            this.changedTextColor = config.ChangedTextColor;
            this.addedTextColor = config.AddedTextColor;
            this.removedTextColor = config.RemovedTextColor;
            this.hyperlinkColor = config.HyperlinkColor;

            if (this.backgroundColor.ToArgb() == Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber)).ToArgb() &&
                this.normalTextColor.ToArgb() == Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber)).ToArgb() &&
                this.changedBackgroundColor.ToArgb() == Color.FromArgb(Int32.Parse("ff003049", NumberStyles.HexNumber)).ToArgb() &&
                this.addedBackgroundColor.ToArgb() == Color.FromArgb(Int32.Parse("ff000000", NumberStyles.HexNumber)).ToArgb() &&
                this.removedBackgroundColor.ToArgb() == Color.FromArgb(Int32.Parse("ff462306", NumberStyles.HexNumber)).ToArgb() &&
                this.changedTextColor.ToArgb() == Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber)).ToArgb() &&
                this.addedTextColor.ToArgb() == Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber)).ToArgb() &&
                this.removedTextColor.ToArgb() == Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber)).ToArgb() &&
                this.hyperlinkColor.ToArgb() == Color.FromArgb(Int32.Parse("ffffffff", NumberStyles.HexNumber)).ToArgb())
            {
                this.DarkMode = true;
                this.btnDefault.Text = "라이트 모드";
            }
        }

        public void SaveConfig(CustomCSSConfig config)
        {
            config.BackgroundColor = this.backgroundColor;
            config.NormalTextColor = this.normalTextColor;
            config.ChangedBackgroundColor = this.changedBackgroundColor;
            config.AddedBackgroundColor = this.addedBackgroundColor;
            config.RemovedBackgroundColor = this.removedBackgroundColor;
            config.ChangedTextColor = this.changedTextColor;
            config.AddedTextColor = this.addedTextColor;
            config.RemovedTextColor = this.removedTextColor;
            config.HyperlinkColor = this.hyperlinkColor;
        }
    }
}
