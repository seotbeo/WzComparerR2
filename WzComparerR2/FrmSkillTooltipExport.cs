using DevComponents.DotNetBar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public partial class FrmSkillTooltipExport : DevComponents.DotNetBar.Office2007Form
    {
        public FrmSkillTooltipExport()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            this.clbJobName.Items.Add("기타", false);
            foreach (var i in jobNameToCode.Keys)
            {
                this.clbJobName.Items.Add(i, false);
            }
        }

        public string ExportFolderPath { get; private set; }
        public List<int> SelectedJobCodes { get; private set; }
        public Wz_Node skillNode { get; set; }
        private bool sorted = false;

        private static Dictionary<string, int[]> jobNameToCode = new Dictionary<string, int[]>()
        {
            { "히어로", new int[] { 100, 110, 111, 112, 114 } }, 
            { "팔라딘", new int[] { 100, 120, 121, 122, 124 } }, 
            { "다크나이트", new int[] { 100, 130, 131, 132, 134 } }, 
            { "아크메이지(불,독)", new int[] { 200, 210, 211, 212, 214 } }, 
            { "아크메이지(썬,콜)", new int[] { 200, 220, 221, 222, 224 } }, 
            { "비숍", new int[] { 200, 230, 231, 232, 234 } }, 
            { "보우마스터", new int[] { 300, 310, 311, 312, 314 } }, 
            { "신궁", new int[] { 300, 320, 321, 322, 324 } }, 
            { "패스파인더", new int[] { 301, 330, 331, 332, 334 } }, 
            { "나이트로드", new int[] { 400, 410, 411, 412, 414 } }, 
            { "섀도어", new int[] { 400, 420, 421, 422, 424 } }, 
            { "듀얼블레이더", new int[] { 400, 430, 431, 432, 433, 434, 436 } }, 
            { "바이퍼", new int[] { 500, 510, 511, 512, 514 } }, 
            { "캡틴", new int[] { 500, 520, 521, 522, 524 } }, 
            { "캐논마스터", new int[] { 501, 530, 531, 532, 534 } }, 
            // { "용의 전인 && 제트", new int[] { 508, 570, 571, 572, 574 } }, 
            { "소울마스터", new int[] { 1000, 1100, 1110, 1111, 1112, 1114 } }, 
            { "플레임위자드", new int[] { 1000, 1200, 1210, 1211, 1212, 1214 } }, 
            { "윈드브레이커", new int[] { 1000, 1300, 1310, 1311, 1312, 1314 } }, 
            { "나이트워커", new int[] { 1000, 1400, 1410, 1411, 1412, 1414 } }, 
            { "스트라이커", new int[] { 1000, 1500, 1510, 1511, 1512, 1514 } }, 
            { "아란", new int[] { 2000, 2100, 2110, 2111, 2112, 2114 } }, 
            { "에반", new int[] { 2001, 2200, 2210, 2211, 2212, 2213, 2214, 2215, 2216, 2217, 2218, 2219, 2220 } }, 
            { "메르세데스", new int[] { 2002, 2300, 2310, 2311, 2312, 2314 } }, 
            { "팬텀", new int[] { 2003, 2400, 2410, 2411, 2412, 2414 } }, 
            { "루미너스", new int[] { 2004, 2700, 2710, 2711, 2712, 2714 } }, 
            { "은월", new int[] { 2005, 2500, 2510, 2511, 2512, 2514 } }, 
            { "데몬 슬레이어", new int[] { 3001, 3100, 3110, 3111, 3112, 3114 } }, 
            { "데몬 어벤져", new int[] { 3001, 3101, 3120, 3121, 3122, 3124 } }, 
            { "블래스터", new int[] { 3000, 3700, 3710, 3711, 3712, 3714 } }, 
            { "배틀메이지", new int[] { 3000, 3200, 3210, 3211, 3212, 3214 } }, 
            { "와일드헌터", new int[] { 3000, 3300, 3310, 3311, 3312, 3314 } }, 
            { "메카닉", new int[] { 3000, 3500, 3510, 3511, 3512, 3514 } }, 
            { "제논", new int[] { 3002, 3600, 3610, 3611, 3612, 3614 } }, 
            { "하야토", new int[] { 4001, 4100, 4110, 4111, 4112, 4114 } }, 
            { "칸나", new int[] { 4002, 4200, 4210, 4211, 4212, 4214 } }, 
            { "미하일", new int[] { 5000, 5100, 5110, 5111, 5112, 5114 } }, 
            { "카이저", new int[] { 6000, 6100, 6110, 6111, 6112, 6114 } }, 
            { "카인", new int[] { 6003, 6300, 6310, 6311, 6312, 6314 } }, 
            { "카데나", new int[] { 6002, 6400, 6410, 6411, 6412, 6414 } }, 
            { "엔젤릭버스터", new int[] { 6001, 6500, 6510, 6511, 6512, 6514 } }, 
            // { "어빌리티", new int[] { 7000 } }, 
            // { "유니온", new int[] { 7100 } }, 
            // { "몬스터라이프", new int[] { 7200 } }, 
            // { "길드", new int[] { 9100 } }, 
            // { "전업기술", new int[] { 9200, 9201, 9202, 9203, 9204 } }, 
            { "제로", new int[] { 10000, 10100, 10110, 10111, 10112, 10114 } }, 
            // { "비스트테이머", new int[] { 11000, 11200, 11210, 11211, 11212 } }, 
            { "카마도 탄지로", new int[] { 12000, 12005, 12100 } }, 
            { "사이타마", new int[] { 12006, 12200 } }, 
            { "핑크빈", new int[] { 13000, 13100 } }, 
            { "예티", new int[] { 13001, 13500 } }, 
            { "키네시스", new int[] { 14000, 14200, 14210, 14211, 14212, 14214 } }, 
            { "아델", new int[] { 15002, 15100, 15110, 15111, 15112, 15114 } }, 
            { "일리움", new int[] { 15000, 15200, 15210, 15211, 15212, 15214 } }, 
            { "칼리", new int[] { 15003, 15400, 15410, 15411, 15412, 15414 } }, 
            { "아크", new int[] { 15001, 15500, 15510, 15511, 15512, 15514 } }, 
            { "렌", new int[] { 16002, 16100, 16110, 16111, 16112, 16114 } }, 
            { "라라", new int[] { 16001, 16200, 16210, 16211, 16212, 16214 } }, 
            { "호영", new int[] { 16000, 16400, 16410, 16411, 16412, 16414 } }, 
            { "묵현", new int[] { 17000, 17500, 17510, 17511, 17512, 17514 } }, 
            { "린", new int[] { 17001, 17200, 17210, 17211, 17212, 17214 } }, 
            // { "에릴 라이트", new int[] { 18001, 18100, 18110, 18111, 18112, 18114 } }, 
            { "시아 아스텔", new int[] { 18000, 18200, 18210, 18211, 18212, 18214 } }, 
            // { "아이엘", new int[] { 18002, 18300, 18310, 18311, 18312, 18314 } }, 
            { "5차(기타)", new int[] { 40000, 40001, 40002, 40003, 40004, 40005 } }, 
            { "6차(기타)", new int[] { 50000, 50006, 50007 } },
        };

        private static Dictionary<string, int[]> jobNameToCodeSorted = new Dictionary<string, int[]>()
        {
            // { "길드", new int[] { 9100 } }, 
            { "나이트로드", new int[] { 400, 410, 411, 412, 414 } }, 
            { "나이트워커", new int[] { 1000, 1400, 1410, 1411, 1412, 1414 } }, 
            { "다크나이트", new int[] { 100, 130, 131, 132, 134 } }, 
            { "데몬 슬레이어", new int[] { 3001, 3100, 3110, 3111, 3112, 3114 } }, 
            { "데몬 어벤져", new int[] { 3001, 3101, 3120, 3121, 3122, 3124 } }, 
            { "듀얼블레이더", new int[] { 400, 430, 431, 432, 433, 434, 436 } }, 
            { "라라", new int[] { 16001, 16200, 16210, 16211, 16212, 16214 } }, 
            { "렌", new int[] { 16002, 16100, 16110, 16111, 16112, 16114 } }, 
            { "루미너스", new int[] { 2004, 2700, 2710, 2711, 2712, 2714 } }, 
            { "린", new int[] { 17001, 17200, 17210, 17211, 17212, 17214 } }, 
            { "메르세데스", new int[] { 2002, 2300, 2310, 2311, 2312, 2314 } }, 
            { "메카닉", new int[] { 3000, 3500, 3510, 3511, 3512, 3514 } }, 
            // { "몬스터라이프", new int[] { 7200 } }, 
            { "묵현", new int[] { 17000, 17500, 17510, 17511, 17512, 17514 } }, 
            { "미하일", new int[] { 5000, 5100, 5110, 5111, 5112, 5114 } }, 
            { "바이퍼", new int[] { 500, 510, 511, 512, 514 } }, 
            { "배틀메이지", new int[] { 3000, 3200, 3210, 3211, 3212, 3214 } }, 
            { "보우마스터", new int[] { 300, 310, 311, 312, 314 } }, 
            { "블래스터", new int[] { 3000, 3700, 3710, 3711, 3712, 3714 } }, 
            { "비숍", new int[] { 200, 230, 231, 232, 234 } }, 
            // { "비스트테이머", new int[] { 11000, 11200, 11210, 11211, 11212 } }, 
            { "사이타마", new int[] { 12006, 12200 } }, 
            { "섀도어", new int[] { 400, 420, 421, 422, 424 } }, 
            { "소울마스터", new int[] { 1000, 1100, 1110, 1111, 1112, 1114 } }, 
            { "스트라이커", new int[] { 1000, 1500, 1510, 1511, 1512, 1514 } }, 
            { "시아 아스텔", new int[] { 18000, 18200, 18210, 18211, 18212, 18214 } }, 
            { "신궁", new int[] { 300, 320, 321, 322, 324 } }, 
            { "아델", new int[] { 15002, 15100, 15110, 15111, 15112, 15114 } }, 
            { "아란", new int[] { 2000, 2100, 2110, 2111, 2112, 2114 } }, 
            // { "아이엘", new int[] { 18002, 18300, 18310, 18311, 18312, 18314 } }, 
            { "아크", new int[] { 15001, 15500, 15510, 15511, 15512, 15514 } }, 
            { "아크메이지(불,독)", new int[] { 200, 210, 211, 212, 214 } }, 
            { "아크메이지(썬,콜)", new int[] { 200, 220, 221, 222, 224 } }, 
            // { "어빌리티", new int[] { 7000 } }, 
            // { "에릴 라이트", new int[] { 18001, 18100, 18110, 18111, 18112, 18114 } }, 
            { "에반", new int[] { 2001, 2200, 2210, 2211, 2212, 2213, 2214, 2215, 2216, 2217, 2218, 2219, 2220 } }, 
            { "엔젤릭버스터", new int[] { 6001, 6500, 6510, 6511, 6512, 6514 } }, 
            { "예티", new int[] { 13001, 13500 } }, 
            { "와일드헌터", new int[] { 3000, 3300, 3310, 3311, 3312, 3314 } }, 
            { "윈드브레이커", new int[] { 1000, 1300, 1310, 1311, 1312, 1314 } }, 
            // { "유니온", new int[] { 7100 } }, 
            { "은월", new int[] { 2005, 2500, 2510, 2511, 2512, 2514 } }, 
            { "일리움", new int[] { 15000, 15200, 15210, 15211, 15212, 15214 } }, 
            // { "전업기술", new int[] { 9200, 9201, 9202, 9203, 9204 } }, 
            { "제논", new int[] { 3002, 3600, 3610, 3611, 3612, 3614 } }, 
            { "제로", new int[] { 10000, 10100, 10110, 10111, 10112, 10114 } }, 
            // { "용의 전인 & 제트", new int[] { 508, 570, 571, 572, 574 } }, 
            { "카데나", new int[] { 6002, 6400, 6410, 6411, 6412, 6414 } }, 
            { "카마도 탄지로", new int[] { 12000, 12005, 12100 } }, 
            { "카이저", new int[] { 6000, 6100, 6110, 6111, 6112, 6114 } }, 
            { "카인", new int[] { 6003, 6300, 6310, 6311, 6312, 6314 } }, 
            { "칸나", new int[] { 4002, 4200, 4210, 4211, 4212, 4214 } }, 
            { "칼리", new int[] { 15003, 15400, 15410, 15411, 15412, 15414 } }, 
            { "캐논마스터", new int[] { 501, 530, 531, 532, 534 } }, 
            { "캡틴", new int[] { 500, 520, 521, 522, 524 } }, 
            { "키네시스", new int[] { 14000, 14200, 14210, 14211, 14212, 14214 } }, 
            { "팔라딘", new int[] { 100, 120, 121, 122, 124 } }, 
            { "패스파인더", new int[] { 301, 330, 331, 332, 334 } }, 
            { "팬텀", new int[] { 2003, 2400, 2410, 2411, 2412, 2414 } }, 
            { "플레임위자드", new int[] { 1000, 1200, 1210, 1211, 1212, 1214 } }, 
            { "핑크빈", new int[] { 13000, 13100 } }, 
            { "하야토", new int[] { 4001, 4100, 4110, 4111, 4112, 4114 } }, 
            { "호영", new int[] { 16000, 16400, 16410, 16411, 16412, 16414 } }, 
            { "히어로", new int[] { 100, 110, 111, 112, 114 } }, 
            { "5차(기타)", new int[] { 40000, 40001, 40002, 40003, 40004, 40005 } }, 
            { "6차(기타)", new int[] { 50000, 50006, 50007 } },
        };

        private static HashSet<int> AllClassesCode()
        {
            HashSet<int> hsClassCode = new HashSet<int>() { };
            foreach (var i in jobNameToCode.Values)
            {
                foreach (var j in i)
                {
                    hsClassCode.Add(j);
                }
            }
            return hsClassCode;
        }

        private void btnSort_Click(object sender, EventArgs e)
        {
            this.sorted = !this.sorted;
            this.btnSort.Text = this.sorted ? "한글 알파벳 순서" : "초기 주문";
            this.clbJobName.Items.Clear();
            this.clbJobName.Items.Add("기타", this.clbJobName.CheckedItems.Contains("기타"));
            var sourceDict = this.sorted ? jobNameToCodeSorted : jobNameToCode;
            foreach (var i in sourceDict.Keys)
            {
                this.clbJobName.Items.Add(i, this.clbJobName.CheckedItems.Contains(i));
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            int checkedCount = this.clbJobName.CheckedItems.Contains("기타") ? this.clbJobName.CheckedItems.Count - 1 : this.clbJobName.CheckedItems.Count;
            bool checkedStatus = (checkedCount < this.clbJobName.Items.Count - 1);
            for (int i = 1; i < this.clbJobName.Items.Count; i++)
            {
                this.clbJobName.SetItemChecked(i, checkedStatus);
            }
        }

        private void btnReverseSelect_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < this.clbJobName.Items.Count; i++)
            {
                this.clbJobName.SetItemChecked(i, !this.clbJobName.GetItemChecked(i));
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (this.clbJobName.CheckedItems.Count == 0)
            {
                MessageBoxEx.Show("내보낼 직업을 하나 이상 선택합니다.", "직업 미선택", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "저장할 폴더를 선택하세요.";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                bool allSelected = this.clbJobName.CheckedItems.Count == this.clbJobName.Items.Count;

                List<int> skillImg = new List<int>() { };
                foreach (Wz_Node node in skillNode.Nodes)
                {
                    Wz_Image currentImg = node.GetValue<Wz_Image>();
                    if (currentImg != null && Int32.TryParse(currentImg.Name.Replace(".img", ""), out int jobCode))
                    {
                        skillImg.Add(jobCode);
                    }
                }
                List<int> selectedJob = skillImg.Intersect(this.clbJobName.CheckedItems.Cast<string>().SelectMany(name => jobNameToCode.ContainsKey(name) ? jobNameToCode[name] : new int[] { }).ToList()).ToList();
                if (this.clbJobName.CheckedItems.Contains("기타"))
                {
                    selectedJob.AddRange(skillImg.Except(AllClassesCode()));
                }
                ExportFolderPath = dlg.SelectedPath;
                SelectedJobCodes = allSelected ? skillImg : selectedJob;
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
