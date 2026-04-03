using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.Config;

namespace WzComparerR2
{
    public partial class FrmOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOptions()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif

            cmbWzEncoding.Items.AddRange(new[]
            {
                new ComboItem("기본"){ Value = 0 },
                new ComboItem("Shift-JIS (JMS)"){ Value = 932 },
                new ComboItem("GB2312 (CMS)"){ Value = 936 },
                new ComboItem("EUC-KR (KMS)"){ Value = 949 },
                new ComboItem("Big5 (TMS)"){ Value = 950 },
                new ComboItem("ISO-8859-1 (GMS)"){ Value = 1252 },
                new ComboItem("ASCII"){ Value = -1 },
            });
        }

        public bool SortWzOnOpened
        {
            get { return chkWzAutoSort.Checked; }
            set { chkWzAutoSort.Checked = value; }
        }

        public bool SortWzByImgID
        {
            get { return chkWzSortByImgID.Checked; }
            set { chkWzSortByImgID.Checked = value; }
        }

        public bool AutoDetectUpdate
        {
            get { return chkAutoDetectUpdate.Checked; }
            set { chkAutoDetectUpdate.Checked = value; }
        }

        public int DefaultWzCodePage
        {
            get
            {
                return ((cmbWzEncoding.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbWzEncoding.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbWzEncoding.SelectedItem = item;
            }
        }

        public bool AutoDetectExtFiles
        {
            get { return chkAutoCheckExtFiles.Checked; }
            set { chkAutoCheckExtFiles.Checked = value; }
        }

        public bool ImgCheckDisabled
        {
            get { return chkImgCheckDisabled.Checked; }
            set { chkImgCheckDisabled.Checked = value; }
        }

        public string NexonOpenAPIKey
        {
            get { return txtOpenAPIKey.Text; }
            set { txtOpenAPIKey.Text = value; }
        }

        public void Load(WcR2Config config)
        {
            this.SortWzOnOpened = config.SortWzOnOpened;
            this.SortWzByImgID = config.SortWzByImgID;
            this.DefaultWzCodePage = config.WzEncoding;
            this.AutoDetectExtFiles = config.AutoDetectExtFiles;
            this.ImgCheckDisabled = config.ImgCheckDisabled;
            this.AutoDetectUpdate = config.AutoDetectUpdate;
            this.NexonOpenAPIKey = config.NexonOpenAPIKey;
        }

        public void Save(WcR2Config config)
        {
            config.SortWzOnOpened = this.SortWzOnOpened;
            config.SortWzByImgID = this.SortWzByImgID;
            config.WzEncoding = this.DefaultWzCodePage;
            config.AutoDetectExtFiles = this.AutoDetectExtFiles;
            config.ImgCheckDisabled = this.ImgCheckDisabled;
            config.AutoDetectUpdate = this.AutoDetectUpdate;
            config.NexonOpenAPIKey = this.NexonOpenAPIKey;
        }

        private void ChkHideAPIKey_Click(object sender, EventArgs e)
        {
            if ((sender as DevComponents.DotNetBar.Controls.CheckBoxX).Checked)
            {
                this.txtOpenAPIKey.PasswordChar = '*';
            }
            else
            {
                this.txtOpenAPIKey.PasswordChar = '\0';
            }
        }
    }
}
