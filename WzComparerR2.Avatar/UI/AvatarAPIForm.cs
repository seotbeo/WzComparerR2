using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.Editors;

namespace WzComparerR2.Avatar.UI
{
    public partial class AvatarAPIForm : DevComponents.DotNetBar.OfficeForm
    {
        public AvatarAPIForm()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif

            cmbRegion.Items.AddRange(new[]
            {
                new ComboItem("KMS"){ Value = 1 },
                //new ComboItem("JMS"){ Value = 2 },
                //new ComboItem("CMS"){ Value = 3 },
                //new ComboItem("GMS(북미)"){ Value = 4 },
                //new ComboItem("GMS(유럽)"){ Value = 5 },
                new ComboItem("MSEA"){ Value = 6 },
                //new ComboItem("TMS"){ Value = 7 },
                //new ComboItem("MSN"){ Value = 8 },
            });
            cmbRegion.SelectedIndex = 0;
        }

        public AvatarAPIForm(string region) : this()
        {
            if (!string.IsNullOrEmpty(region))
            {
                selectedRegion = region;
            }    
        }

        public string CharaName
        {
            get { return textBoxX1.Text; }
            set { textBoxX1.Text = value; }
        }

        public bool Type1
        {
            get { return checkBoxX1.Checked; }
        }

        public string selectedRegion
        {
            get
            {
                return cmbRegion.SelectedItem.ToString();
            }
            set
            {
                for (int i = 0; i < cmbRegion.Items.Count; i++)
                {
                    if ((cmbRegion.Items[i] as ComboItem).Text == value)
                    {
                        cmbRegion.SelectedIndex = i;
                        return;
                    }
                }
                cmbRegion.SelectedIndex = -1;
            }
        }

        private void textBoxX1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void cmbRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboItem selectedItem = cmbRegion.SelectedItem as ComboItem;
            switch ((int)selectedItem.Value)
            {
                case 1:
                    labelX1.Enabled = true;
                    checkBoxX1.Enabled = true;
                    checkBoxX2.Enabled = true;
                    break;
                default:
                    labelX1.Enabled = false;
                    checkBoxX1.Enabled = false;
                    checkBoxX2.Enabled = false;
                    break;
            }
        }
    }
}