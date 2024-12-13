using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.Config;
using System.Security.Policy;
using System.IO;
using Spine;
using Newtonsoft.Json.Linq;

namespace WzComparerR2
{
    public partial class FrmOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOptions()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
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

            cmbWzVersionVerifyMode.Items.AddRange(new[]
            {
                new ComboItem("빠름"){ Value = WzLib.WzVersionVerifyMode.Fast },
                new ComboItem("기본"){ Value = WzLib.WzVersionVerifyMode.Default },
            });

            cmbDesiredLanguage.Items.AddRange(new[]
            {
                new ComboItem("광둥어 (HKMS)"){ Value = "yue" },
                new ComboItem("영어 (GMS/MSEA)"){ Value = "en" },
                new ComboItem("일본어 (JMS)"){ Value = "ja" },
                new ComboItem("중국어 간체 (CMS)"){ Value = "zh-CN" },
                new ComboItem("중국어 번체 (TMS)"){ Value = "zh-TW" },
                new ComboItem("한국어 (KMS)"){ Value = "ko" },
            });

            cmbMozhiBackend.Items.AddRange(new[]
            {
                new ComboItem("mozhi.aryak.me"){ Value = "https://mozhi.aryak.me" },
                new ComboItem("translate.bus-hit.me"){ Value = "https://translate.bus-hit.me" },
                new ComboItem("nyc1.mz.ggtyler.dev"){ Value = "https://nyc1.mz.ggtyler.dev" },
                new ComboItem("translate.projectsegfau.lt"){ Value = "https://translate.projectsegfau.lt" },
                new ComboItem("translate.nerdvpn.de"){ Value = "https://translate.nerdvpn.de" },
                new ComboItem("mozhi.ducks.party"){ Value = "https://mozhi.ducks.party" },
                new ComboItem("mozhi.frontendfriendly.xyz"){ Value = "https://mozhi.frontendfriendly.xyz" },
                new ComboItem("mozhi.pussthecat.org"){ Value = "https://mozhi.pussthecat.org" },
                new ComboItem("mo.zorby.top"){ Value = "https://mo.zorby.top" },
                new ComboItem("mozhi.adminforge.de"){ Value = "https://mozhi.adminforge.de" },
                new ComboItem("translate.privacyredirect.com"){ Value = "https://translate.privacyredirect.com" },
                new ComboItem("mozhi.canine.tools"){ Value = "https://mozhi.canine.tools" },
                new ComboItem("mozhi.gitro.xyz"){ Value = "https://mozhi.gitro.xyz" },
                new ComboItem("api.hikaricalyx.com"){ Value = "https://api.hikaricalyx.com/mozhi" },
            });

            cmbPreferredTranslateEngine.Items.AddRange(new[]
            {
                new ComboItem("Google (Non-Mozhi)"){ Value = 0 },
                new ComboItem("Google"){ Value = 1 },
                new ComboItem("DeepL"){ Value = 2 },
                new ComboItem("DuckDuckGo / Bing"){ Value = 3 },
                new ComboItem("MyMemory"){ Value = 4 },
                new ComboItem("Yandex"){ Value = 5 },
                new ComboItem("Naver Papago (Non-Mozhi)"){ Value = 6 },
            });

            cmbPreferredLayout.Items.AddRange(new[]
            {
                new ComboItem("번역하지 않음"){ Value = 0 },
                new ComboItem("번역 문자열 우선"){ Value = 1 },
                new ComboItem("원래 문자열 우선"){ Value = 2 },
                new ComboItem("번역 문자열만"){ Value = 3 },
            });

            cmbDetectCurrency.Items.AddRange(new[]
            {
                new ComboItem("자동 감지"){ Value = "auto" },
                new ComboItem("대만 달러 (NTD)"){ Value = "twd" },
                new ComboItem("미국 달러 (USD)"){ Value = "usd" },
                new ComboItem("싱가포르 달러 (SGD)"){ Value = "sgd" },
                new ComboItem("일본 엔 (JPY)"){ Value = "jpy" },
                new ComboItem("중국 위안 (CNY)"){ Value = "cny" },
                new ComboItem("한국 원 (KRW)"){ Value = "krw" },
            });

            cmbDesiredCurrency.Items.AddRange(new[]
            {
                new ComboItem("변환하지 않음"){ Value = "none" },
                new ComboItem("대만 달러 (NTD)"){ Value = "twd" },
                new ComboItem("말레이시아 링깃 (MYR)"){ Value = "myr" },
                new ComboItem("마카오 파타카 (MOP)"){ Value = "mop" },
                new ComboItem("미국 달러 (USD)"){ Value = "usd" },
                new ComboItem("싱가포르 달러 (SGD)"){ Value = "sgd" },
                new ComboItem("유로 (EUR)"){ Value = "eur" },
                new ComboItem("일본 엔 (JPY)"){ Value = "jpy" },
                new ComboItem("중국 위안 (CNY)"){ Value = "cny" },
                new ComboItem("한국 원 (KRW)"){ Value = "krw" },
                new ComboItem("호주 달러 (AUD)"){ Value = "aud" },
                new ComboItem("홍콩 달러 (HKD)"){ Value = "hkd" },
                new ComboItem("캐나다 달러 (CAD)"){ Value = "cad" },
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

        public string NxSecretKey
        {
            get { return txtSecretkey.Text; }
            set { txtSecretkey.Text = value;}
        }

        public int PreferredLayout
        {
            get
            {
                return ((cmbPreferredLayout.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbPreferredLayout.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbPreferredLayout.SelectedItem = item;
            }
        }

        public string MozhiBackend
        {
            get
            {
                return ((cmbMozhiBackend.SelectedItem as ComboItem)?.Value as string) ?? "https://mozhi.aryak.me";
            }
            set
            {
                var items = cmbMozhiBackend.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbMozhiBackend.SelectedItem = item;
            }
        }

        public string DetectCurrency
        {
            get
            {
                return ((cmbDetectCurrency.SelectedItem as ComboItem)?.Value as string) ?? "auto";
            }
            set
            {
                var items = cmbDetectCurrency.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbDetectCurrency.SelectedItem = item;
            }
        }

        public string DesiredCurrency
        {
            get
            {
                return ((cmbDesiredCurrency.SelectedItem as ComboItem)?.Value as string) ?? "jpy";
            }
            set
            {
                var items = cmbDesiredCurrency.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbDesiredCurrency.SelectedItem = item;
            }
        }

        public int PreferredTranslateEngine
        {
            get
            {
                return ((cmbPreferredTranslateEngine.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbPreferredTranslateEngine.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbPreferredTranslateEngine.SelectedItem = item;
            }
        }

        public string DesiredLanguage
        {
            get
            {
                return ((cmbDesiredLanguage.SelectedItem as ComboItem)?.Value as string) ?? "ja";
            }
            set
            {
                var items = cmbDesiredLanguage.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbDesiredLanguage.SelectedItem = item;
            }
        }

        private void buttonXCheck2_Click(object sender, EventArgs e)
        {
            string respText;
            var req = WebRequest.Create((cmbMozhiBackend.SelectedItem as ComboItem)?.Value + "/api/engines") as HttpWebRequest;
            req.Timeout = 15000;
            try
            {
                string respJson = new StreamReader(req.GetResponse().GetResponseStream(), Encoding.UTF8).ReadToEnd();
                if (respJson.Contains("All Engines"))
                {
                    respText = "Mozhi 서버가 유효합니다.";
                }
                else
                {
                    respText = "Mozhi 서버가 유효하지 않습니다.";
                }
            }
            catch (WebException ex)
            {
                string respJson = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                respText = "Mozhi 서버가 유효하지 않습니다.";
            }
            catch (Exception ex)
            {
                respText = "알 수 없는 오류가 발생했습니다.: " + ex;
            }
            MessageBoxEx.Show(respText);
        }

        private void buttonXCheck3_Click(object sender, EventArgs e)
        {
            string respText;
            JObject testJObject;
            try
            {
                testJObject = JObject.Parse(txtSecretkey.Text);
                respText = "JSON이 유효합니다. ";
            }
            catch
            {
                respText = "JSON이 유효하지 않습니다. ";
            }
            MessageBoxEx.Show(respText);
        }

        public WzLib.WzVersionVerifyMode WzVersionVerifyMode
        {
            get { return ((cmbWzVersionVerifyMode.SelectedItem as ComboItem)?.Value as WzLib.WzVersionVerifyMode?) ?? default; }
            set
            {
                var items = cmbWzVersionVerifyMode.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as WzLib.WzVersionVerifyMode? == value)
                    ?? items.First();
                cmbWzVersionVerifyMode.SelectedItem = item;
            }
        }

        public void Load(WcR2Config config)
        {
            this.SortWzOnOpened = config.SortWzOnOpened;
            this.SortWzByImgID = config.SortWzByImgID;
            this.DefaultWzCodePage = config.WzEncoding;
            this.AutoDetectExtFiles = config.AutoDetectExtFiles;
            this.ImgCheckDisabled = config.ImgCheckDisabled;
            this.WzVersionVerifyMode = config.WzVersionVerifyMode;
            this.NxSecretKey = config.NxSecretKey;
            this.MozhiBackend = config.MozhiBackend;
            this.PreferredTranslateEngine = config.PreferredTranslateEngine;
            this.DesiredLanguage = config.DesiredLanguage;
            this.PreferredLayout = config.PreferredLayout;
            this.DetectCurrency = config.DetectCurrency;
            this.DesiredCurrency = config.DesiredCurrency;
        }

        public void Save(WcR2Config config)
        {
            config.SortWzOnOpened = this.SortWzOnOpened;
            config.SortWzByImgID = this.SortWzByImgID;
            config.WzEncoding = this.DefaultWzCodePage;
            config.AutoDetectExtFiles = this.AutoDetectExtFiles;
            config.ImgCheckDisabled = this.ImgCheckDisabled;
            config.WzVersionVerifyMode = this.WzVersionVerifyMode;
            config.NxSecretKey = this.NxSecretKey;
            config.MozhiBackend = this.MozhiBackend;
            config.PreferredTranslateEngine = this.PreferredTranslateEngine;
            config.DesiredLanguage = this.DesiredLanguage;
            config.PreferredLayout = this.PreferredLayout;
            config.DetectCurrency = this.DetectCurrency;
            config.DesiredCurrency = this.DesiredCurrency;
        }
    }
}
