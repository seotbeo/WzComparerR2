using DevComponents.DotNetBar;
using System;
using System.Windows.Forms;
using WzComparerR2.PluginBase;

namespace WzComparerR2.DB2
{
    /// <summary>
    /// MapleStoryDB2 外掛進入點。
    ///
    /// 提供三個以清單／圖示瀏覽 WZ 內容的視窗：
    /// MapleStoryDB2（分類資料表）、圖示預覽、圖片瀏覽。
    /// 功能移植自 WzComparerR2-Plus (WzComparerR2++)。
    /// </summary>
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {
        }

        private RibbonBar barDB2;

        private DB2Form db2Form;
        private IconsForm iconsForm;
        private ImageViewerForm imageViewerForm;

        protected override void OnLoad()
        {
            Db2Host.Context = this.Context;

            // 三個按鈕共用同一個 RibbonBar（標題為 DB2），而非各自佔一欄。
            this.barDB2 = AddButtons("DB2",
                ("Maple DB2", (EventHandler)btnDB2_Click),
                ("아이콘 뷰어", btnIcons_Click),
                ("이미지 뷰어", btnPicViewer_Click));

            this.Context.MainStyleChanged += Context_MainStyleChanged;
        }

        protected override void OnUnload()
        {
            this.Context.MainStyleChanged -= Context_MainStyleChanged;

            MapRenderLauncher.Close();

            DisposeForm(ref this.db2Form);
            DisposeForm(ref this.iconsForm);
            DisposeForm(ref this.imageViewerForm);

            Db2Host.Context = null;
        }

        /// <summary>
        /// 主程式切換樣式（例如切到 VisualStudio2012Dark）時，
        /// 讓已經開啟的視窗跟著換色。
        /// </summary>
        private void Context_MainStyleChanged(object sender, EventArgs e)
        {
            if (this.db2Form != null && !this.db2Form.IsDisposed)
            {
                this.db2Form.ApplyTheme();
            }
            if (this.iconsForm != null && !this.iconsForm.IsDisposed)
            {
                this.iconsForm.ApplyTheme();
            }
            if (this.imageViewerForm != null && !this.imageViewerForm.IsDisposed)
            {
                this.imageViewerForm.ApplyTheme();
            }
        }

        /// <summary>
        /// 建立一個 RibbonBar，並把所有按鈕垂直堆在同一欄裡。
        ///
        /// 版面沿用主程式 MainForm 的 ribbonBar9（遊戲更新程式）作法：
        /// 外層一個 LayoutOrientation = Vertical 的 ItemContainer，
        /// 每個按鈕再各包一層 ItemContainer 當作一列。
        /// </summary>
        private RibbonBar AddButtons(string barText, params (string text, EventHandler onClick)[] buttons)
        {
            var bar = this.Context.AddRibbonBar("Tools", barText);
            bar.AutoOverflowEnabled = true;
            bar.ContainerControlProcessDialogKey = true;

            var rows = new ItemContainer
            {
                Name = "itemContainerDB2",
                LayoutOrientation = eOrientation.Vertical,
            };

            foreach (var def in buttons)
            {
                var button = new ButtonItem("", def.text)
                {
                    SubItemsExpandWidth = 16,
                };
                button.Click += def.onClick;

                var row = new ItemContainer();
                row.SubItems.Add(button);
                rows.SubItems.Add(row);
            }

            bar.Items.Add(rows);
            // RibbonPanel 只會自動量測 Dock 過的 bar，外掛的 bar 是絕對定位，
            // 所以自己依內容算一次寬度。
            bar.Width = Math.Max(1, bar.GetAutoSizeWidth());
            return bar;
        }

        private void btnDB2_Click(object sender, EventArgs e)
        {
            ShowForm(ref this.db2Form, () => new DB2Form());
        }

        private void btnIcons_Click(object sender, EventArgs e)
        {
            if (Db2Host.RequireBaseWz())
            {
                ShowForm(ref this.iconsForm, () => new IconsForm());
            }
        }

        private void btnPicViewer_Click(object sender, EventArgs e)
        {
            if (Db2Host.RequireBaseWz())
            {
                ShowForm(ref this.imageViewerForm, () => new ImageViewerForm());
            }
        }

        private static void ShowForm<T>(ref T form, Func<T> factory) where T : Form
        {
            if (form == null || form.IsDisposed)
            {
                form = factory();
            }
            form.Show();
            form.BringToFront();
        }

        private static void DisposeForm<T>(ref T form) where T : Form
        {
            if (form != null)
            {
                if (!form.IsDisposed)
                {
                    // 各表單的 FormClosing 會取消關閉改為隱藏，因此直接 Dispose。
                    form.Dispose();
                }
                form = null;
            }
        }
    }
}
