using DevComponents.DotNetBar;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WzComparerR2.DB2
{
    /// <summary>
    /// 深色樣式（VisualStudio2012Dark）的配色套用工具。
    ///
    /// 表單本身與 DotNetBar 控制項（ButtonX / ComboBoxEx / LabelX / TextBoxX /
    /// PanelEx / SuperTabControl）會自動跟著 StyleManager 換色，
    /// 但 DataGridView、ListBox 這類原生 WinForms 控制項不會，必須在這裡手動指定。
    /// 色碼沿用主程式 MainForm.UpdateButtonItemStyles 的設定。
    /// </summary>
    internal static class Db2Theme
    {
        /// <summary>VisualStudio2012Dark 的內容區底色 (#2D2D30)。</summary>
        public static readonly Color DarkBackColor = Color.FromArgb(-13816528);
        public static readonly Color DarkForeColor = Color.LightGray;
        public static readonly Color DarkGridColor = Color.FromArgb(63, 63, 70);
        public static readonly Color DarkSelectionBackColor = Color.FromArgb(38, 79, 120);
        public static readonly Color DarkHeaderBackColor = Color.FromArgb(37, 37, 38);

        /// <summary>主程式目前是否為深色樣式。</summary>
        public static bool IsDarkMode
        {
            get { return Db2Host.Context?.IsDarkMode ?? false; }
        }

        public static void Apply(Form form)
        {
            if (form == null)
            {
                return;
            }
            // Office2007Form 會自行跟著 StyleManager 上色，這裡只處理非 DotNetBar 的子控制項。
            ApplyToChildren(form);
        }

        private static void ApplyToChildren(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case DataGridView grid:
                        Apply(grid);
                        break;

                    case ListBox listBox:
                        Apply(listBox);
                        break;

                    case ScrollBar scrollBar:
                        ApplyScrollBarTheme(scrollBar);
                        break;

                    case LabelX labelX:
                        // LabelX 的 ForeColor 一旦被設定就不再跟著樣式走，所以兩種樣式都要明確指定。
                        labelX.ForeColor = IsDarkMode ? DarkForeColor : SystemColors.ControlText;
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyToChildren(control);
                }
            }
        }

        public static void Apply(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }

            bool dark = IsDarkMode;
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = dark ? DarkBackColor : SystemColors.AppWorkspace;
            grid.GridColor = dark ? DarkGridColor : SystemColors.ControlDark;

            // 預設的 3D 外框在深色下會畫成亮灰色，直接拿掉改用格線顏色。
            grid.BorderStyle = dark ? BorderStyle.None : BorderStyle.FixedSingle;
            grid.ColumnHeadersBorderStyle = dark
                ? DataGridViewHeaderBorderStyle.Single
                : DataGridViewHeaderBorderStyle.Raised;
            grid.RowHeadersBorderStyle = grid.ColumnHeadersBorderStyle;

            // DataGridView 的捲軸是它自己的子控制項，必須個別套用深色主題。
            foreach (Control child in grid.Controls)
            {
                if (child is ScrollBar scrollBar)
                {
                    ApplyScrollBarTheme(scrollBar);
                }
            }

            grid.DefaultCellStyle.BackColor = dark ? DarkBackColor : Color.White;
            grid.DefaultCellStyle.ForeColor = dark ? DarkForeColor : Color.Black;
            grid.DefaultCellStyle.SelectionBackColor = dark ? DarkSelectionBackColor : Color.LightCyan;
            grid.DefaultCellStyle.SelectionForeColor = dark ? Color.White : Color.Black;

            grid.ColumnHeadersDefaultCellStyle.BackColor = dark ? DarkHeaderBackColor : SystemColors.Control;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = dark ? DarkForeColor : SystemColors.ControlText;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = grid.ColumnHeadersDefaultCellStyle.ForeColor;

            grid.RowHeadersDefaultCellStyle.BackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.RowHeadersDefaultCellStyle.ForeColor = grid.ColumnHeadersDefaultCellStyle.ForeColor;
        }

        public static void Apply(ListBox listBox)
        {
            if (listBox == null)
            {
                return;
            }

            bool dark = IsDarkMode;
            listBox.BackColor = dark ? DarkBackColor : Color.White;
            listBox.ForeColor = dark ? DarkForeColor : Color.Black;
            listBox.BorderStyle = dark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;

            // ListBox 的捲軸屬於視窗本身（WS_VSCROLL），主題要套在自己的 handle 上。
            ApplyScrollBarTheme(listBox);
        }

        /// <summary>
        /// 讓原生捲軸跟著深色。Windows 10 1809 以後 uxtheme 提供 "DarkMode_Explorer"
        /// 這組主題，套不上時 SetWindowTheme 只會回傳失敗碼，不影響其他繪製。
        ///
        /// 注意 SetWindowTheme 位於 uxtheme.dll；DotNetBar 內部把它宣告成 user32，
        /// 主程式的 Dotnet6Patch 就是為了繞開那個壞掉的宣告。
        /// </summary>
        private static void ApplyScrollBarTheme(Control control)
        {
            if (control == null || !control.IsHandleCreated)
            {
                return;
            }

            try
            {
                SetWindowTheme(control.Handle, IsDarkMode ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch (EntryPointNotFoundException)
            {
            }
            catch (DllNotFoundException)
            {
            }
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        /// <summary>
        /// 快顯選單同樣不會跟著 StyleManager，深色樣式下必須換掉繪製器，
        /// 否則會以系統色畫成白底。
        /// </summary>
        public static void Apply(ToolStrip toolStrip)
        {
            if (toolStrip == null)
            {
                return;
            }

            if (IsDarkMode)
            {
                toolStrip.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
                toolStrip.BackColor = DarkHeaderBackColor;
                toolStrip.ForeColor = DarkForeColor;
            }
            else
            {
                toolStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
                toolStrip.BackColor = SystemColors.Control;
                toolStrip.ForeColor = SystemColors.ControlText;
            }
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => DarkHeaderBackColor;
            public override Color ImageMarginGradientBegin => DarkHeaderBackColor;
            public override Color ImageMarginGradientMiddle => DarkHeaderBackColor;
            public override Color ImageMarginGradientEnd => DarkHeaderBackColor;
            public override Color MenuItemSelected => DarkSelectionBackColor;
            public override Color MenuItemSelectedGradientBegin => DarkSelectionBackColor;
            public override Color MenuItemSelectedGradientEnd => DarkSelectionBackColor;
            public override Color MenuItemBorder => DarkSelectionBackColor;
            public override Color MenuBorder => DarkGridColor;
            public override Color SeparatorDark => DarkGridColor;
            public override Color SeparatorLight => DarkGridColor;
        }
    }
}
