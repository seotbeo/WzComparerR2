using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using DevComponents.DotNetBar;
using WzComparerR2.Controls;

namespace WzComparerR2.PluginBase
{
    public class PluginContext
    {
        internal PluginContext(PluginContextProvider contextProvider)
        {
            this.contextProvider = contextProvider;
        }

        private PluginContextProvider contextProvider;

        public Form MainForm
        {
            get { return this.contextProvider.MainForm; }
        }

        public DotNetBarManager DotNetBarManager
        {
            get { return this.contextProvider.DotNetBarManager; }
        }

        public Wz_Node SelectedNode1
        {
            get { return this.contextProvider.SelectedNode1; }
        }

        public Wz_Node SelectedNode2
        {
            get { return this.contextProvider.SelectedNode2; }
        }

        public Wz_Node SelectedNode3
        {
            get { return this.contextProvider.SelectedNode3; }
        }

        public SuperTabItem SelectedTab
        {
            get { return this.SuperTabControl1.SelectedTab; }
        }

        public event EventHandler<WzNodeEventArgs> SelectedNode1Changed
        {
            add { contextProvider.SelectedNode1Changed += value; }
            remove { contextProvider.SelectedNode1Changed -= value; }
        }

        public event EventHandler<WzNodeEventArgs> SelectedNode2Changed
        {
            add { contextProvider.SelectedNode2Changed += value; }
            remove { contextProvider.SelectedNode2Changed -= value; }
        }

        public event EventHandler<WzNodeEventArgs> SelectedNode3Changed
        {
            add { contextProvider.SelectedNode3Changed += value; }
            remove { contextProvider.SelectedNode3Changed -= value; }
        }

        public event EventHandler<WzStructureEventArgs> WzOpened
        {
            add { contextProvider.WzOpened += value; }
            remove { contextProvider.WzOpened-= value; }
        }

        public event EventHandler<WzStructureEventArgs> WzClosing
        {
            add { contextProvider.WzClosing += value; }
            remove { contextProvider.WzClosing -= value; }
        }

        public StringLinker DefaultStringLinker
        {
            get { return this.contextProvider.DefaultStringLinker; }
        }

        public AlphaForm DefaultTooltipWindow
        {
            get { return this.contextProvider.DefaultTooltipWindow; }
        }

        /// <summary>
        /// 主視窗目前套用的 DotNetBar 樣式。
        /// </summary>
        public eStyle MainStyle
        {
            get { return this.contextProvider.MainStyle; }
        }

        /// <summary>
        /// 主視窗目前是否為深色樣式（VisualStudio2012Dark）。
        /// </summary>
        public bool IsDarkMode
        {
            get { return this.MainStyle == eStyle.VisualStudio2012Dark; }
        }

        /// <summary>
        /// 主視窗樣式變更時發生，外掛可藉此同步更新自己的視窗配色。
        /// </summary>
        public event EventHandler MainStyleChanged
        {
            add { contextProvider.MainStyleChanged += value; }
            remove { contextProvider.MainStyleChanged -= value; }
        }

        /// <summary>
        /// 在主視窗的 WZ 樹狀圖中選取指定節點，使其成為目前節點。
        /// </summary>
        /// <param name="node">要選取的節點。</param>
        /// <returns>成功選取時傳回 true；找不到節點所屬的 WZ 或路徑時傳回 false。</returns>
        public bool SelectNode(Wz_Node node)
        {
            if (node == null)
            {
                return false;
            }
            return this.contextProvider.SelectNode(node);
        }

        private SuperTabControl SuperTabControl1
        {
            get
            {
                var controls = this.contextProvider.MainForm.Controls.Find("superTabControl1", true);
                SuperTabControl tabControl = controls.Length > 0 ? (controls[0] as SuperTabControl) : null;
                return tabControl;
            }
        }

        public void AddRibbonBar(string tabName, RibbonBar ribbonBar)
        {
            RibbonControl ribbonCtrl = this.MainForm.Controls["ribbonControl1"] as RibbonControl;

            if (ribbonCtrl == null)
            {
                throw new Exception("无法找到RibbonContainer。");
            }

            RibbonPanel ribbonPanel = null;
            RibbonTabItem tabItem;
            foreach (BaseItem item in ribbonCtrl.Items)
            {
                if ((tabItem = item as RibbonTabItem) != null
                    && string.Equals(Convert.ToString(tabItem.Tag), tabName, StringComparison.OrdinalIgnoreCase))
                {
                    ribbonPanel = tabItem.Panel;
                    break;
                }
            }

            if (ribbonPanel == null)
            {
                throw new Exception("无法找到RibbonPanel。");
            }

            Control lastBar = ribbonPanel.Controls[0];
            ribbonBar.Location = new System.Drawing.Point(lastBar.Location.X + lastBar.Width, lastBar.Location.Y);
            ribbonBar.Size = new System.Drawing.Size(Math.Max(1, ribbonBar.Width), lastBar.Height);
            ribbonPanel.SuspendLayout();
            ribbonPanel.Controls.Add(ribbonBar);
            ribbonPanel.Controls.SetChildIndex(ribbonBar, 0);
            ribbonPanel.ResumeLayout(false);
        }

        public RibbonBar AddRibbonBar(string tabName, string barText)
        {
            RibbonBar bar = new RibbonBar();
            bar.Text = barText;
            AddRibbonBar(tabName, bar);
            return bar;
        }

        public void AddTab(string tabName, SuperTabControlPanel tabPanel)
        {
            SuperTabControl tabControl = this.SuperTabControl1;
            
            if (tabControl == null)
            {
                throw new Exception("无法找到SuperTabControl。");
            }

            tabControl.SuspendLayout();

            SuperTabItem tabItem = new SuperTabItem();
            tabControl.Controls.Add(tabPanel);

            tabControl.Tabs.Add(tabItem);
            tabPanel.TabItem = tabItem;

            tabItem.Text = tabName;
            tabItem.AttachedControl = tabPanel;
            tabControl.ResumeLayout(false);
        }

        public SuperTabControlPanel AddTab(string tabName)
        {
            SuperTabControlPanel panel = new SuperTabControlPanel();

            AddTab(tabName, panel);
            panel.Controls.Add(new Button());
            return panel;
        }
    }
}
