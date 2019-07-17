using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DevComponents.AdvTree;

namespace WzComparerR2
{
    public partial class FrmAbout : DevComponents.DotNetBar.Office2007Form
    {
        public FrmAbout()
        {
            InitializeComponent();

            this.lblClrVer.Text = string.Format("{0} ({1})", Environment.Version, Environment.Is64BitProcess ? "x64": "x86");
            this.lblAsmVer.Text = GetAsmVersion().ToString();
            this.lblFileVer.Text = GetFileVersion().ToString();
            this.lblCopyright.Text = GetAsmCopyright().ToString();
            GetPluginInfo();
        }

        private Version GetAsmVersion()
        {
            return this.GetType().Assembly.GetName().Version;
        }

        private string GetFileVersion()
        {
            return FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location).FileVersion;
        }

        private string GetAsmCopyright()
        {
            Assembly asm = this.GetType().Assembly;
            object[] attri = asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return (attri == null || attri.Length <= 0) ? string.Empty : (attri[0] as AssemblyCopyrightAttribute).Copyright;
        }

        private void GetPluginInfo()
        {
            this.advTree1.Nodes.Clear();
            this.advTree1.Nodes.Add(new Node(string.Format("KMS-MixEff <font color=\"#808080\">Beta v0.0.1</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[MixEff] 믹스 염색, 아이템 이펙트 출력 <font color=\"#808080\">옴마</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("KMS <font color=\"#808080\">v3.1.1</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 각종 기능 추가, 최종 번역 <font color=\"#808080\">박현민</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 문자열 번역 <font color=\"#808080\">슈린냥</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 문자열 오류 제보 <font color=\"#808080\">인소야닷컴 실버</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 문자열 오류 제보 <font color=\"#808080\">jusir_@naver.com</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 장비 툴팁 오류 제보 <font color=\"#808080\">@Sunaries</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 중복 착용 불가 문자열 오류 제보 <font color=\"#808080\">인소야닷컴 진류</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 아바타 저장 기능 추가 <font color=\"#808080\">@craftingmod</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 아바타 불러오기 오류 제보 <font color=\"#808080\">인소야닷컴 일감</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 파일 저장시 이름 규칙 오류 제보 <font color=\"#808080\">@mabooky</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 아바타 하이레프 귀 오류 제보 <font color=\"#808080\">메이플인벤 누리신드롬</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 패치와 함께 비교시 오류 제보 <font color=\"#808080\">@Sunaries</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 사용 가능 직업 문구 오류 제보 <font color=\"#808080\">@tanyoucai</font>")));
            this.advTree1.Nodes.Add(new Node(string.Format("[KMS] 퀘스트 상태 파티클 미적용 오류 제보 <font color=\"#808080\">메이플인벤 펄더</font>")));

            if (PluginBase.PluginManager.LoadedPlugins.Count > 0)
            {
                foreach (var plugin in PluginBase.PluginManager.LoadedPlugins)
                {
                    string nodeTxt = string.Format("{0} <font color=\"#808080\">{1} ({2})</font>",
                        plugin.Instance.Name, 
                        plugin.Instance.Version,
                        plugin.Instance.FileVersion);
                    Node node = new Node(nodeTxt);
                    this.advTree1.Nodes.Add(node);
                }
            }
            else
            {
                string nodeTxt = "<font color=\"#808080\">연결된 플러그인 없음</font>";
                Node node = new Node(nodeTxt);
                this.advTree1.Nodes.Add(node);
            }
        }

    }
}
