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

            this.comboBoxEx3.Items.AddRange((new[] { "검은색", "빨간색", "주황색", "노란색", "초록색", "파란색", "보라색", "갈색" }).Select(color =>
            {
                var comboBoxItem = new DevComponents.DotNetBar.ComboBoxItem();
                comboBoxItem.Text = color;
                return comboBoxItem;
            }).ToArray());

            this.comboBoxEx4.Items.AddRange((new[] { "검은색", "파란색", "빨간색", "초록색", "갈색", "에메랄드", "보라색", "자수정" }).Select(color =>
            {
                var comboBoxItem = new DevComponents.DotNetBar.ComboBoxItem();
                comboBoxItem.Text = color;
                return comboBoxItem;
            }).ToArray());

            this.comboBoxEx3.SelectedIndex = 0;
            this.comboBoxEx4.SelectedIndex = 0;
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

        public int PreferredStringCopyMethod
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

        public bool CopyParsedSkillString
        {
            get { return chkCopyParsedSkillString.Checked; }
            set { chkCopyParsedSkillString.Checked = value; }
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

        public void Load(CharaSimConfig config)
        {
            this.PreferredStringCopyMethod = config.PreferredStringCopyMethod;
            this.CopyParsedSkillString = config.CopyParsedSkillString;
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
            config.PreferredStringCopyMethod = this.PreferredStringCopyMethod;
            config.CopyParsedSkillString = this.CopyParsedSkillString;
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
