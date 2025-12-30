using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using DevComponents.Editors;
using WzComparerR2.Config;
using WzComparerR2.AvatarCommon;


namespace WzComparerR2
{
    public partial class FrmQuickViewSetting : DevComponents.DotNetBar.Office2007Form
    {
        public FrmQuickViewSetting()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.comboBoxEx1.SelectedIndex = 0;
            this.comboBoxEx2.SelectedIndex = 0;

            cmbPreferredStringCopyMethod.Items.AddRange(new[]
                {
                new ComboItem("Raw String") { Value = 0 },
                new ComboItem("Plain String") { Value = 1 },
                //new ComboItem("MapleWiki Optimized") { Value = 2 },
            });

            this.comboBoxEx3.Items.AddRange(AvatarCanvas.HairColor.Select(color =>
            {
                var comboBoxItem = new DevComponents.DotNetBar.ComboBoxItem();
                comboBoxItem.Text = color;
                return comboBoxItem;
            }).ToArray());

            this.comboBoxEx4.Items.AddRange(AvatarCanvas.FaceColor.Select(color =>
            {
                var comboBoxItem = new DevComponents.DotNetBar.ComboBoxItem();
                comboBoxItem.Text = color;
                return comboBoxItem;
            }).ToArray());

            this.comboBoxEx3.SelectedIndex = 0;
            this.comboBoxEx4.SelectedIndex = 0;
            this.chkShowMiniMapMob.Enabled = this.chkShowMiniMap.Checked;
            this.chkShowMiniMapNpc.Enabled = this.chkShowMiniMap.Checked;
            this.chkShowMiniMapPortal.Enabled = this.chkShowMiniMap.Checked;
        }

        [Link]
        public bool Skill_ShowProperties
        {
            get { return checkBoxX10.Checked; }
            set { checkBoxX10.Checked = value; }
        }

        [Link]
        public bool Skill_ShowID
        {
            get { return checkBoxX1.Checked; }
            set { checkBoxX1.Checked = value; }
        }

        [Link]
        public bool Skill_ShowDelay
        {
            get { return checkBoxX2.Checked; }
            set { checkBoxX2.Checked = value; }
        }

        [Link]
        public bool Skill_ShowArea
        {
            get { return checkBoxX16.Checked; }
            set { checkBoxX16.Checked = value; }
        }

        [Link]
        public DefaultLevel Skill_DefaultLevel
        {
            get { return (DefaultLevel)comboBoxEx1.SelectedIndex; }
            set { comboBoxEx1.SelectedIndex = (int)value; }
        }

        [Link]
        public int Skill_IntervalLevel
        {
            get { return Convert.ToInt32(((ComboItem)comboBoxEx2.SelectedItem).Text); }
            set
            {
                for (int i = 0; i < comboBoxEx2.Items.Count; i++)
                {
                    if (value <= Convert.ToInt32(((ComboItem)comboBoxEx2.Items[i]).Text))
                    {
                        comboBoxEx2.SelectedIndex = i;
                        return;
                    }
                }
                comboBoxEx2.SelectedIndex = comboBoxEx2.Items.Count - 1;
            }
        }

        [Link]
        public bool Skill_DisplayCooltimeMSAsSec
        {
            get { return checkBoxX13.Checked; }
            set { checkBoxX13.Checked = value; }
        }

        [Link]
        public bool Skill_DisplayPermyriadAsPercent
        {
            get { return checkBoxX14.Checked; }
            set { checkBoxX14.Checked = value; }
        }

        [Link]
        public bool Skill_IgnoreEvalError
        {
            get { return checkBoxX15.Checked; }
            set { checkBoxX15.Checked = value; }
        }

        [Link]
        public bool Skill_ShowReqSkill
        {
            get { return chkShowReqSkill.Checked; }
            set { chkShowReqSkill.Checked = value; }
        }

        [Link]
        public bool Gear_ShowID
        {
            get { return checkBoxX3.Checked; }
            set { checkBoxX3.Checked = value; }
        }

        [Link]
        public bool Gear_ShowWeaponSpeed
        {
            get { return checkBoxX4.Checked; }
            set { checkBoxX4.Checked = value; }
        }

        [Link]
        public bool Item_ShowID
        {
            get { return checkBoxX5.Checked; }
            set { checkBoxX5.Checked = value; }
        }

        [Link]
        public bool DamageSkin_ShowDamageSkinID
        {
            get { return chkShowDamageSkinID.Checked; }
            set { chkShowDamageSkinID.Checked = value; }
        }

        [Link]
        public bool DamageSkin_ShowDamageSkin
        {
            get { return chkShowDamageSkin.Checked; }
            set { chkShowDamageSkin.Checked = value; }
        }

        [Link]
        public bool DamageSkin_UseMiniSize
        {
            get { return chkUseMiniSize.Checked; }
            set { chkUseMiniSize.Checked = value; }
        }

        [Link]
        public bool DamageSkin_AlwaysUseMseaFormat
        {
            get { return chkAlwaysUseMseaFormat.Checked; }
            set { chkAlwaysUseMseaFormat.Checked = value; }
        }

        [Link]
        public bool DamageSkin_DisplayUnitOnSingleLine
        {
            get { return chkDisplayUnitOnSingleLine.Checked; }
            set { chkDisplayUnitOnSingleLine.Checked = value; }
        }

        [Link]
        public long DamageSkin_DamageSkinNumber
        {
            get { return long.TryParse(txtDamageSkinNumber.Text, out long val) ? val : 0; }
            set { txtDamageSkinNumber.Text = value.ToString(); }
        }

        [Link]
        public bool Gear_ShowLevelOrSealed
        {
            get { return checkBoxX6.Checked; }
            set { checkBoxX6.Checked = value; }
        }

        [Link]
        public bool Gear_ShowMedalTag
        {
            get { return checkBoxX11.Checked; }
            set { checkBoxX11.Checked = value; }
        }

        [Link]
        public bool Gear_MaxStar25
        {
            get { return checkBoxX17.Checked; }
            set { checkBoxX17.Checked = value; }
        }

        [Link]
        public bool Gear_ShowCosmetic
        {
            get { return checkBoxX18.Checked; }
            set { checkBoxX18.Checked = value; }
        }


        [Link]
        public bool Recipe_ShowID
        {
            get { return checkBoxX7.Checked; }
            set { checkBoxX7.Checked = value; }
        }

        [Link]
        public bool Item_LinkRecipeInfo
        {
            get { return checkBoxX8.Checked; }
            set { checkBoxX8.Checked = value; }
        }

        [Link]
        public bool Item_LinkRecipeItem
        {
            get { return checkBoxX9.Checked; }
            set { checkBoxX9.Checked = value; }
        }

        [Link]
        public bool Item_ShowNickTag
        {
            get { return checkBoxX12.Checked; }
            set { checkBoxX12.Checked = value; }
        }

        [Link]
        public int Item_CosmeticHairColor
        {
            get { return comboBoxEx3.SelectedIndex; }
            set { comboBoxEx3.SelectedIndex = value; }
        }

        [Link]
        public int Item_CosmeticFaceColor
        {
            get { return comboBoxEx4.SelectedIndex; }
            set { comboBoxEx4.SelectedIndex = value; }
        }

        [Link]
        public bool Map_ShowMiniMap
        {
            get { return chkShowMiniMap.Checked; }
            set { chkShowMiniMap.Checked = value; }
        }

        [Link]
        public bool Map_ShowMiniMapMob
        {
            get { return chkShowMiniMapMob.Checked; }
            set { chkShowMiniMapMob.Checked = value; }
        }

        [Link]
        public bool Map_ShowMiniMapNpc
        {
            get { return chkShowMiniMapNpc.Checked; }
            set { chkShowMiniMapNpc.Checked = value; }
        }

        [Link]
        public bool Map_ShowMiniMapPortal
        {
            get { return chkShowMiniMapPortal.Checked; }
            set { chkShowMiniMapPortal.Checked = value; }
        }

        [Link]
        public bool Npc_ShowAllIllustAtOnce
        {
            get { return chkShowAllIllustAtOnce.Checked; }
            set { chkShowAllIllustAtOnce.Checked = value; }
        }

        [Link]
        public int Quest_DefaultState
        {
            get { return comboBoxExQuestState.SelectedIndex; }
            set { comboBoxExQuestState.SelectedIndex = value; }
        }

        [Link]
        public bool Quest_ShowAllStates
        {
            get { return chkQAS.Checked; }
            set { chkQAS.Checked = value; }
        }

        [Link]
        public bool Misc_Enable22AniStyle
        {
            get { return chkEnable22AniStyle.Checked; }
            set { chkEnable22AniStyle.Checked = value; }
        }

        [Link]
        public int Misc_PreferredStringCopyMethod
        {
            get
            {
                return ((cmbPreferredStringCopyMethod.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbPreferredStringCopyMethod.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbPreferredStringCopyMethod.SelectedItem = item;
            }
        }
        
        [Link]
        public bool Misc_EnableWorldArchive
        {
            get { return chkEnableWorldArchive.Checked; }
            set { chkEnableWorldArchive.Checked = value; }
        }

        public void Load(CharaSimConfig config)
        {
            var linkProp = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttributes(typeof(LinkAttribute), false).Length > 0);

            foreach (var prop in linkProp)
            {
                string[] path = prop.Name.Split('_');
                try
                {
                    var configGroup = config.GetType().GetProperty(path[0]).GetValue(config, null);
                    var configPropInfo = configGroup.GetType().GetProperty(path[1]);
                    var value = configPropInfo.GetGetMethod().Invoke(configGroup, null);
                    prop.GetSetMethod().Invoke(this, new object[] { value });
                }
                catch { }
            }
        }

        public void Save(CharaSimConfig config)
        {
            var linkProp = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.GetCustomAttributes(typeof(LinkAttribute), false).Length > 0);

            foreach (var prop in linkProp)
            {
                string[] path = prop.Name.Split('_');
                try
                {
                    var configGroup = config.GetType().GetProperty(path[0]).GetValue(config, null);
                    var configPropInfo = configGroup.GetType().GetProperty(path[1]);
                    var value = prop.GetGetMethod().Invoke(this, null);
                    configPropInfo.GetSetMethod().Invoke(configGroup, new object[] { value });
                }
                catch { }
            }
        }

        private void ChkShowMiniMap_CheckedChanged(object sender, System.EventArgs e)
        {
            this.chkShowMiniMapMob.Enabled = this.chkShowMiniMap.Checked;
            this.chkShowMiniMapNpc.Enabled = this.chkShowMiniMap.Checked;
            this.chkShowMiniMapPortal.Enabled = this.chkShowMiniMap.Checked;
        }

        private void ChkQAS_CheckedChanged(object sender, System.EventArgs e)
        {
            this.comboBoxExQuestState.Enabled = !this.chkQAS.Checked;
            this.labelXQS.Enabled = !this.chkQAS.Checked;
            this.labelXQSHint.Enabled = !this.chkQAS.Checked;
        }

        private void txtDamageSkinNumber_TextChanged(object sender, EventArgs e)
        {
            this.buttonX1.Enabled = !(string.IsNullOrEmpty(txtDamageSkinNumber.Text) || txtDamageSkinNumber.Text == "0");

            string digitsOnly = new string(txtDamageSkinNumber.Text.Where(char.IsDigit).ToArray());

            if (txtDamageSkinNumber.Text != digitsOnly)
            {
                int cursorPos = txtDamageSkinNumber.SelectionStart;
                txtDamageSkinNumber.Text = digitsOnly;
                txtDamageSkinNumber.SelectionStart = Math.Min(cursorPos, txtDamageSkinNumber.Text.Length);
            }
        }

        private sealed class LinkAttribute : Attribute
        {
        }
    }

    public enum DefaultLevel
    {
        Level0 = 0,
        Level1 = 1,
        LevelMax = 2,
        LevelMaxWithCO = 3,
    }
}
