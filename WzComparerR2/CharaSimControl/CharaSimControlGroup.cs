using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.Controls;
using WzComparerR2.WzLib;
using WzComparerR2.Config;

namespace WzComparerR2.CharaSimControl
{
    public class CharaSimControlGroup
    {
        public CharaSimControlGroup()
        {
            tooltip = new AfrmTooltip();
            tooltip.TopMost = true;
        }

        private AfrmTooltip quickView;
        private AfrmTooltip tooltip;
        private AfrmItem frmItem;
        private AfrmStat frmStat;
        private AfrmEquip frmEquip;
        private Character character;
        private StringLinker stringLinker;

        public AfrmTooltip TooltipQuickView
        {
            get
            {
                if (quickView == null)
                {
                    quickView = new AfrmTooltip();
                    quickView.ObjectMouseMove += new ObjectMouseEventHandler(frmQuickView_ObjectMouseMove);
                    quickView.ObjectMouseLeave += new EventHandler(frmQuickView_ObjectMouseLeave);
                }
                return quickView;
            }
        }

        public AfrmItem UIItem
        {
            get
            {
                if (frmItem == null)
                {
                    frmItem = new AfrmItem();
                    frmItem.KeyDown += new KeyEventHandler(afrm_KeyDown);
                    frmItem.MouseDown += new MouseEventHandler(frmItem_MouseDown);
                    frmItem.DragOver += new DragEventHandler(frmItem_DragOver);
                    frmItem.DragDrop += new DragEventHandler(frmItem_DragDrop);
                    frmItem.ItemMouseMove += new ItemMouseEventHandler(frmItem_ItemMouseMove);
                    frmItem.ItemMouseLeave += new EventHandler(frmItem_ItemMouseLeave);
                    frmItem.Character = this.character;
                }
                return frmItem;
            }
        }

        public AfrmStat UIStat
        {
            get
            {
                if (frmStat == null)
                {
                    frmStat = new AfrmStat();
                    frmStat.KeyDown += new KeyEventHandler(afrm_KeyDown);
                    frmStat.ObjectMouseMove += new ObjectMouseEventHandler(frmStat_ObjectMouseMove);
                    frmStat.ObjectMouseLeave += new EventHandler(frmStat_ObjectMouseLeave);
                    frmStat.Character = this.character;
                }
                return frmStat;
            }
        }

        public AfrmEquip UIEquip
        {
            get
            {
                if (frmEquip == null)
                {
                    frmEquip = new AfrmEquip();
                    frmEquip.KeyDown += new KeyEventHandler(afrm_KeyDown);
                    frmEquip.MouseDown += new MouseEventHandler(frmEquip_MouseDown);
                    frmEquip.DragOver += new DragEventHandler(frmEquip_DragOver);
                    frmEquip.DragDrop += new DragEventHandler(frmEquip_DragDrop);
                    frmEquip.Character = this.character;
                }
                return frmEquip;
            }
        }

        public Character Character
        {
            get { return character; }
            set
            {
                this.character = value;
                this.tooltip.Character = value;
                if (frmEquip != null)
                    this.frmEquip.Character = value;
                if (frmStat != null)
                    this.frmStat.Character = value;
                if (frmEquip != null)
                    this.frmEquip.Character = value;
            }
        }

        public StringLinker StringLinker
        {
            get { return stringLinker; }
            set
            {
                this.stringLinker = value;
                this.tooltip.StringLinker = value;
            }
        }

        private void afrm_KeyDown(object sender, KeyEventArgs e)
        {
            Form frm = sender as Form;
            if (frm == null)
                return;

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    frm.Hide();
                    break;
                case Keys.F1:
                    break;
                case Keys.Up:
                    frm.Top -= 1;
                    break;
                case Keys.Down:
                    frm.Top += 1;
                    break;
                case Keys.Left:
                    frm.Left -= 1;
                    break;
                case Keys.Right:
                    frm.Left += 1;
                    break;
                case Keys.I:
                    frmItem.Visible = !frmItem.Visible;
                    break;
                case Keys.S:
                    frmStat.Visible = !frmStat.Visible;
                    break;
                case Keys.E:
                    frmEquip.Visible = !frmEquip.Visible;
                    break;
            }
        }

        private void frmItem_MouseDown(object sender, MouseEventArgs e)
        {
            ItemBase dragItem = frmItem.GetItemByPoint(e.Location);

            if (dragItem != null)
            {
                if (Control.ModifierKeys == Keys.Control)
                {
                    int originIdx = Array.IndexOf<ItemBase>(frmItem.SelectedTab.Items, dragItem);
                    if (originIdx > -1)
                    {
                        frmItem.SelectedTab.Items[originIdx] = null;
                        frmItem.Refresh();
                    }
                }
                else
                {
                    frmItem.DoDragDrop(dragItem, DragDropEffects.Move);
                    tooltip.Visible = false;
                }
            }
        }

        private ItemBase getDragDataItem(IDataObject data)
        {
            ItemBase dragItem;
            if (data != null
                && ((dragItem = data.GetData(typeof(Item)) as Item) != null
                || (dragItem = data.GetData(typeof(Gear)) as Gear) != null))
            {
                return dragItem;
            }
            return null;
        }

        private void frmItem_DragOver(object sender, DragEventArgs e)
        {
            ItemBase dragItem;
            int idx;
            if ((e.AllowedEffect & DragDropEffects.Move) != 0
                && (idx = frmItem.GetItemIndexByPoint(frmItem.PointToClient(new Point(e.X, e.Y)))) != -1
                && (dragItem = getDragDataItem(e.Data)) != null)
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void frmItem_DragDrop(object sender, DragEventArgs e)
        {
            if ((e.Effect & (DragDropEffects.Move)) == 0)
            {
                return;
            }

            ItemBase dragItem;
            int dropIdx, originIdx;
            if ((dragItem = getDragDataItem(e.Data)) == null
                || (originIdx = Array.IndexOf<ItemBase>(frmItem.SelectedTab.Items, dragItem)) == -1)
            {
                return;
            }
            dropIdx = frmItem.GetItemIndexByPoint(frmItem.PointToClient(new Point(e.X, e.Y)));
            if (dropIdx == -1) //移除装备
            {
                frmItem.SelectedTab.Items[originIdx] = null;
            }
            else if (originIdx != dropIdx)
            {
                frmItem.SelectedTab.Items[originIdx] = frmItem.SelectedTab.Items[dropIdx];
                frmItem.SelectedTab.Items[dropIdx] = dragItem;
            }
            else
            {
                return;
            }
            frmItem.Refresh();
        }

        private void frmItem_ItemMouseMove(object sender, ItemMouseEventArgs e)
        {
            if (e.Item == null)
            {
                tooltip.Visible = false;
                return;
            }
            if (e.Item != tooltip.TargetItem)
            {
                tooltip.TargetItem = e.Item;
                tooltip.Refresh();
            }
            Point pos = frmItem.PointToScreen(e.Location);
            pos.Offset(5, 5);
            tooltip.Location = pos;
            tooltip.Visible = true;
            tooltip.BringToFront();
        }

        private void frmItem_ItemMouseLeave(object sender, EventArgs e)
        {
            tooltip.Visible = false;
        }

        private void frmStat_ObjectMouseMove(object sender, ObjectMouseEventArgs e)
        {
            if (e.Obj is Skill && !this.stringLinker.HasValues)
            {
                this.stringLinker.Load(PluginBase.PluginManager.FindWz(Wz_Type.String).GetValueEx<Wz_File>(null),
                    PluginBase.PluginManager.FindWz(Wz_Type.Item).GetValueEx<Wz_File>(null),
                    PluginBase.PluginManager.FindWz(Wz_Type.Etc).GetValueEx<Wz_File>(null),
                    PluginBase.PluginManager.FindWz(Wz_Type.Quest).GetValueEx<Wz_File>(null));
            }
            if (e.Obj == null)
            {
                tooltip.Visible = false;
                return;
            }
            if (e.Obj != tooltip.TargetItem)
            {
                tooltip.TargetItem = e.Obj;
                tooltip.Refresh();
            }
            Point pos = frmStat.PointToScreen(e.Location);
            pos.Offset(5, 5);
            tooltip.Location = pos;
            tooltip.Visible = true;
            tooltip.BringToFront();
        }

        private void frmStat_ObjectMouseLeave(object sender, EventArgs e)
        {
            tooltip.Visible = false;
        }

        private void frmQuickView_ObjectMouseMove(object sender, ObjectMouseEventArgs e)
        {
            if (e.Obj == null)
            {
                tooltip.Visible = false;
                return;
            }
            if (e.Obj != tooltip.TargetItem)
            {
                tooltip.TargetItem = e.Obj;
                tooltip.Refresh();
            }
            Point pos = quickView.PointToScreen(e.Location);
            pos.Offset(5, 5);
            tooltip.Location = pos;
            tooltip.Visible = true;
            tooltip.BringToFront();
        }

        private void frmQuickView_ObjectMouseLeave(object sender, EventArgs e)
        {
            tooltip.Visible = false;
        }

        private void frmEquip_MouseDown(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void frmEquip_DragOver(object sender, DragEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void frmEquip_DragDrop(object sender, DragEventArgs e)
        {
            //throw new NotImplementedException();
        }

        public void UpdateSettings(CharaSimConfig Setting)
        {
            tooltip.SkillRender.ShowProperties = false;
            tooltip.SkillRender.ShowObjectID = false;
            tooltip.SkillRender.ShowDelay = false;
            tooltip.SkillRender.ShowArea = false;
            tooltip.SkillRender.ShowReqSkill = Setting.Skill.ShowReqSkill;
            tooltip.SkillRender.DisplayCooltimeMSAsSec = Setting.Skill.DisplayCooltimeMSAsSec;
            tooltip.SkillRender.DisplayPermyriadAsPercent = Setting.Skill.DisplayPermyriadAsPercent;
            tooltip.SkillRender.IgnoreEvalError = Setting.Skill.IgnoreEvalError;
            tooltip.SkillRender.LevelViewMode = (SkillLevelViewMode)Setting.Skill.SkillLevelViewMode;

            tooltip.GearRender.ShowObjectID = false;
            tooltip.GearRender.ShowSpeed = Setting.Gear.ShowWeaponSpeed;
            tooltip.GearRender.ShowLevelOrSealed = false;
            tooltip.GearRender.MaxStar25 = Setting.Gear.MaxStar25;
            tooltip.GearRender.ShowCosmetic = false;
            tooltip.GearRender22.ShowObjectID = false;
            tooltip.GearRender22.ShowSpeed = true;
            tooltip.GearRender22.ShowLevelOrSealed = false;
            tooltip.GearRender22.MaxStar25 = Setting.Gear.MaxStar25;
            tooltip.GearRender22.ShowCosmetic = false;

            tooltip.ItemRender.ShowObjectID = false;
            tooltip.ItemRender.LinkRecipeInfo = false;
            tooltip.ItemRender.LinkRecipeItem = Setting.Item.LinkRecipeItem;
            tooltip.ItemRender.ShowLevelOrSealed = false;
            tooltip.ItemRender.CosmeticHairColor = Setting.Item.CosmeticHairColor;
            tooltip.ItemRender.CosmeticFaceColor = Setting.Item.CosmeticFaceColor;
            tooltip.ItemRender22.ShowObjectID = false;
            tooltip.ItemRender22.LinkRecipeInfo = false;
            tooltip.ItemRender22.LinkRecipeItem = Setting.Item.LinkRecipeItem;
            tooltip.ItemRender22.ShowLevelOrSealed = false;
            tooltip.ItemRender22.CosmeticHairColor = Setting.Item.CosmeticHairColor;
            tooltip.ItemRender22.CosmeticFaceColor = Setting.Item.CosmeticFaceColor;

            tooltip.RecipeRender.ShowObjectID = false;

            tooltip.Enable22AniStyle = Setting.Misc.Enable22AniStyle;
        }
    }
}
