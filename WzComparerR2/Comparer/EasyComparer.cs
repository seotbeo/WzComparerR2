using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.CharaSimControl;
using WzComparerR2.CharaSim;
using System.Text.RegularExpressions;
using WzComparerR2.Config;

namespace WzComparerR2.Comparer
{
    public class EasyComparer
    {
        public EasyComparer()
        {
            this.Comparer = new WzFileComparer();
        }

        private Wz_Node[] WzNewOld { get; set; } = new Wz_Node[2];
        private Wz_File[] WzFileNewOld { get; set; } = new Wz_File[2];
        private StringLinker[] StringLinkerNewOld { get; set; } = new StringLinker[2];
        private SortedSet<int> OutputGearTooltipIDs { get; set; } = new SortedSet<int>();
        private SortedSet<int> OutputItemTooltipIDs { get; set; } = new SortedSet<int>();
        private SortedSet<int> OutputMapTooltipIDs { get; set; } = new SortedSet<int>();
        private SortedSet<int> OutputSkillTooltipIDs { get; set; } = new SortedSet<int>();
        private Dictionary<string, HashSet<string>> DiffSkillTags { get; set; } = new Dictionary<string, HashSet<string>>();

        public WzFileComparer Comparer { get; protected set; }
        private string stateInfo;
        private string stateDetail;
        public bool OutputPng { get; set; }
        public bool OutputAddedImg { get; set; }
        public bool OutputRemovedImg { get; set; }
        public bool EnableDarkMode { get; set; }
        public bool OutputGearTooltip { get; set; }
        public bool OutputItemTooltip { get; set; }
        public bool OutputMapTooltip { get; set; }
        public bool OutputSkillTooltip { get; set; }
        public bool HashPngFileName { get; set; }

        public string StateInfo
        {
            get { return stateInfo; }
            set
            {
                stateInfo = value;
                this.OnStateInfoChanged(EventArgs.Empty);
            }
        }

        public string StateDetail
        {
            get { return stateDetail; }
            set
            {
                stateDetail = value;
                this.OnStateDetailChanged(EventArgs.Empty);
            }
        }

        public event EventHandler StateInfoChanged;
        public event EventHandler StateDetailChanged;
        public event EventHandler<Patcher.PatchingEventArgs> PatchingStateChanged;

        protected virtual void OnStateInfoChanged(EventArgs e)
        {
            if (this.StateInfoChanged != null)
                this.StateInfoChanged(this, e);
        }

        protected virtual void OnStateDetailChanged(EventArgs e)
        {
            if (this.StateDetailChanged != null)
                this.StateDetailChanged(this, e);
        }

        protected virtual void OnPatchingStateChanged(Patcher.PatchingEventArgs e)
        {
            if (this.PatchingStateChanged != null)
                this.PatchingStateChanged(this, e);
        }

        public void EasyCompareWzFiles(Wz_File fileNew, Wz_File fileOld, string outputDir, StreamWriter index = null)
        {
            StateInfo = "Wz 비교중...";
           
            if ((fileNew.Type == Wz_Type.Base || fileOld.Type == Wz_Type.Base) && index == null) //至少有一个base 拆分对比
            {
                var virtualNodeNew = RebuildWzFile(fileNew);
                var virtualNodeOld = RebuildWzFile(fileOld);
                WzFileComparer comparer = new WzFileComparer();
                comparer.IgnoreWzFile = true;

                if (OutputSkillTooltip || OutputItemTooltip || OutputGearTooltip || OutputMapTooltip)
                {
                    this.WzNewOld[0] = fileNew.Node;
                    this.WzNewOld[1] = fileOld.Node;

                    this.WzFileNewOld[0] = fileNew.Node.GetNodeWzFile();
                    this.WzFileNewOld[1] = fileOld.Node.GetNodeWzFile();

                    for (var i = 0; i < 2; i++)
                    {
                        this.StringLinkerNewOld[i] = new StringLinker();
                        this.StringLinkerNewOld[i].Load(WzNewOld[i]?.FindNodeByPath("String").GetNodeWzFile(),
                            WzNewOld[i]?.FindNodeByPath("Item").GetNodeWzFile(),
                            WzNewOld[i]?.FindNodeByPath("Etc").GetNodeWzFile());
                    }
                }

                var dictNew = SplitVirtualNode(virtualNodeNew);
                var dictOld = SplitVirtualNode(virtualNodeOld);

                //寻找共同wzType
                var wzTypeList = dictNew.Select(kv => kv.Key)
                    .Where(wzType => dictOld.ContainsKey(wzType));

                CreateStyleSheet(outputDir);

                string htmlFilePath = Path.Combine(outputDir, "index.html");

                FileStream htmlFile = null;
                StreamWriter sw = null;
                StateInfo = "Index 문서 작성중...";
                StateDetail = "문서 구조 생성중";
                try
                {
                    htmlFile = new FileStream(htmlFilePath, FileMode.Create, FileAccess.Write);
                    sw = new StreamWriter(htmlFile, Encoding.UTF8);
                    sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                    sw.WriteLine("<html>");
                    sw.WriteLine("<head>");
                    sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
                    sw.WriteLine("<title>Index {0}←{1}</title>", fileNew.Header.WzVersion, fileOld.Header.WzVersion);
                    sw.WriteLine("<link type=\"text/css\" rel=\"stylesheet\" href=\"style.css\" />");
                    sw.WriteLine("</head>");
                    sw.WriteLine("<body>");
                    //输出概况
                    sw.WriteLine("<p class=\"wzf\">");
                    sw.WriteLine("<table>");
                    sw.WriteLine("<tr><th>파일명</th><th>신버전 용량</th><th>구버전 용량</th><th>변경</th><th>추가</th><th>제거</th></tr>");
                    foreach (var wzType in wzTypeList)
                    {
                        var vNodeNew = dictNew[wzType];
                        var vNodeOld = dictOld[wzType];
                        var cmp = comparer.Compare(vNodeNew, vNodeOld);
                        OutputFile(vNodeNew.LinkNodes.Select(node => node.Value).OfType<Wz_File>().ToList(),
                            vNodeOld.LinkNodes.Select(node => node.Value).OfType<Wz_File>().ToList(),
                            wzType,
                            cmp.ToList(),
                            outputDir,
                            sw);
                    }
                    sw.WriteLine("</table>");
                    sw.WriteLine("</p>");

                    //html结束
                    sw.WriteLine("</body>");
                    sw.WriteLine("</html>");
                }
                finally
                {
                    try
                    {
                        if (sw != null)
                        {
                            sw.Flush();
                            sw.Close();
                        }
                    }
                    catch
                    {
                    }
                }
            }
            else //执行传统对比
            {
                WzFileComparer comparer = new WzFileComparer();
                comparer.IgnoreWzFile = false;
                var cmp = comparer.Compare(fileNew.Node, fileOld.Node);
                CreateStyleSheet(outputDir);
                OutputFile(fileNew, fileOld, fileNew.Type, cmp.ToList(), outputDir, index);
            }

            GC.Collect();
        }

        public void EasyCompareWzStructures(Wz_Structure structureNew, Wz_Structure structureOld, string outputDir, StreamWriter index)
        {
            var virtualNodeNew = RebuildWzStructure(structureNew);
            var virtualNodeOld = RebuildWzStructure(structureOld);
            WzFileComparer comparer = new WzFileComparer();
            comparer.IgnoreWzFile = true;

            var dictNew = SplitVirtualNode(virtualNodeNew);
            var dictOld = SplitVirtualNode(virtualNodeOld);

            //寻找共同wzType
            var wzTypeList = dictNew.Select(kv => kv.Key)
                .Where(wzType => dictOld.ContainsKey(wzType));

            CreateStyleSheet(outputDir);

            foreach (var wzType in wzTypeList)
            {
                var vNodeNew = dictNew[wzType];
                var vNodeOld = dictOld[wzType];
                var cmp = comparer.Compare(vNodeNew, vNodeOld);
                OutputFile(vNodeNew.LinkNodes.Select(node => node.Value).OfType<Wz_File>().ToList(),
                    vNodeOld.LinkNodes.Select(node => node.Value).OfType<Wz_File>().ToList(),
                    wzType,
                    cmp.ToList(),
                    outputDir,
                    index);
            }
        }

        public void EasyCompareWzStructuresToWzFiles(Wz_File fileNew, Wz_Structure structureOld, string outputDir, StreamWriter index)
        {
            var virtualNodeOld = RebuildWzStructure(structureOld);
            WzFileComparer comparer = new WzFileComparer();
            comparer.IgnoreWzFile = true;

            var dictOld = SplitVirtualNode(virtualNodeOld);

            //寻找共同wzType
            var wzTypeList = dictOld.Select(kv => kv.Key)
                .Where(wzType => dictOld.ContainsKey(wzType));

            CreateStyleSheet(outputDir);

            foreach (var wzType in wzTypeList)
            {
                var vNodeOld = dictOld[wzType];
                var cmp = comparer.Compare(fileNew.Node, vNodeOld);
                OutputFile(new List<Wz_File>() { fileNew },
                    vNodeOld.LinkNodes.Select(node => node.Value).OfType<Wz_File>().ToList(),
                    wzType,
                    cmp.ToList(),
                    outputDir,
                    index);
            }
        }

        private WzVirtualNode RebuildWzFile(Wz_File wzFile)
        {
            //分组
            List<Wz_File> subFiles = new List<Wz_File>();
            WzVirtualNode topNode = new WzVirtualNode(wzFile.Node);

            foreach (var childNode in wzFile.Node.Nodes)
            {
                var subFile = childNode.GetValue<Wz_File>();
                if (subFile != null && !subFile.IsSubDir) //wz子文件
                {
                    subFiles.Add(subFile);
                }
                else //其他
                {
                    topNode.AddChild(childNode, true);
                }
            }

            if (wzFile.Type == Wz_Type.Base)
            {
                foreach (var grp in subFiles.GroupBy(f => f.Type))
                {
                    WzVirtualNode fileNode = new WzVirtualNode();
                    fileNode.Name = grp.Key.ToString();
                    foreach (var file in grp)
                    {
                        fileNode.Combine(file.Node);
                    }
                    topNode.AddChild(fileNode);
                }
            }
            return topNode;
        }

        private WzVirtualNode RebuildWzStructure(Wz_Structure wzStructure)
        {
            //分组
            List<Wz_File> subFiles = wzStructure.wz_files.Where(wz_file => wz_file != null).ToList();
            WzVirtualNode topNode = new WzVirtualNode();

            foreach (var grp in subFiles.GroupBy(f => f.Type))
            {
                WzVirtualNode fileNode = new WzVirtualNode();
                fileNode.Name = grp.Key.ToString();
                foreach (var file in grp)
                {
                    fileNode.Combine(file.Node);
                }
                topNode.AddChild(fileNode);
            }
            return topNode;
        }

        private Dictionary<Wz_Type, WzVirtualNode> SplitVirtualNode(WzVirtualNode node)
        {
            var dict = new Dictionary<Wz_Type, WzVirtualNode>();
            Wz_File wzFile = null;
            if (node.LinkNodes.Count > 0)
            {
                wzFile = node.LinkNodes[0].Value as Wz_File;
                dict[wzFile.Type] = node;
            }

            if (wzFile?.Type == Wz_Type.Base || node.LinkNodes.Count == 0) //额外处理
            {
                var wzFileList = node.ChildNodes
                    .Select(child => new { Node = child, WzFile = child.LinkNodes[0].Value as Wz_File })
                    .Where(item => item.WzFile != null);

                foreach (var item in wzFileList)
                {
                    dict[item.WzFile.Type] = item.Node;
                }
            }

            return dict;
        }

        private IEnumerable<string> GetFileInfo(Wz_File wzf, Func<Wz_File, string> extractor)
        {
            IEnumerable<string> result = new[] { extractor.Invoke(wzf) }
                .Concat(wzf.MergedWzFiles.Select(extractor.Invoke));

            if (wzf.Type != Wz_Type.Base)
            {
                result = result.Concat(wzf.Node.Nodes.Where(n => n.Value is Wz_File).SelectMany(nwzf => GetFileInfo((Wz_File)nwzf.Value, extractor)));
            }

            return result;
        }

        private void OutputFile(Wz_File fileNew, Wz_File fileOld, Wz_Type type, List<CompareDifference> diffLst, string outputDir, StreamWriter index)
        {
            OutputFile(new List<Wz_File>() { fileNew },
                new List<Wz_File>() { fileOld },
                type,
                diffLst,
                outputDir,
                index);
        }
        private void OutputFile(List<Wz_File> fileNew, List<Wz_File> fileOld, Wz_Type type, List<CompareDifference> diffLst, string outputDir, StreamWriter index = null)
        {
            string htmlFilePath = Path.Combine(outputDir, type.ToString() + ".html");
            for (int i = 1; File.Exists(htmlFilePath); i++)
            {
                htmlFilePath = Path.Combine(outputDir, string.Format("{0}_{1}.html", type, i));
            }
            string srcDirPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(htmlFilePath) + "_files");
            if (OutputPng && !Directory.Exists(srcDirPath))
            {
                Directory.CreateDirectory(srcDirPath);
            }

            FileStream htmlFile = null;
            StreamWriter sw = null;
            StateInfo = type + " 문서 작성중...";
            StateDetail = "문서 구조 생성중";
            try
            {
                htmlFile = new FileStream(htmlFilePath, FileMode.Create, FileAccess.Write);
                sw = new StreamWriter(htmlFile, Encoding.UTF8);
                sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                sw.WriteLine("<html>");
                sw.WriteLine("<head>");
                sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
                sw.WriteLine("<title>{0} {1}←{2}</title>", type, fileNew[0].GetMergedVersion(), fileOld[0].GetMergedVersion());
                sw.WriteLine("<link type=\"text/css\" rel=\"stylesheet\" href=\"style.css\" />");
                sw.WriteLine("</head>");
                sw.WriteLine("<body>");
                //输出概况
                sw.WriteLine("<p class=\"wzf\">");
                sw.WriteLine("<table>");
                sw.WriteLine("<tr><th>&nbsp;</th><th>파일명</th><th>용량</th><th>버전</th></tr>");
                sw.WriteLine("<tr><td>신버전</td><td>{0}</td><td>{1}</td><td>{2}</td></tr>",
                    string.Join("<br/>", fileNew.SelectMany(wzf => GetFileInfo(wzf, ewzf => ewzf.Header.FileName))),
                    string.Join("<br/>", fileNew.SelectMany(wzf => GetFileInfo(wzf, ewzf => ewzf.Header.FileSize.ToString("N0")))),
                    string.Join("<br/>", fileNew.Select(wzf => wzf.GetMergedVersion()))
                    );
                sw.WriteLine("<tr><td>구버전</td><td>{0}</td><td>{1}</td><td>{2}</td></tr>",
                    string.Join("<br/>", fileOld.SelectMany(wzf => GetFileInfo(wzf, ewzf => ewzf.Header.FileName))),
                    string.Join("<br/>", fileOld.SelectMany(wzf => GetFileInfo(wzf, ewzf => ewzf.Header.FileSize.ToString("N0")))),
                    string.Join("<br/>", fileOld.Select(wzf => wzf.GetMergedVersion()))
                    );
                sw.WriteLine("<tr><td>현재시각</td><td colspan='3'>{0:yyyy-MM-dd HH:mm:ss.fff}</td></tr>", DateTime.Now);
                sw.WriteLine("<tr><td>옵션</td><td colspan='3'>{0}</td></tr>", string.Join("<br/>", new[] {
                    this.OutputPng ? "-OutputPng" : null,
                    this.OutputAddedImg ? "-OutputAddedImg" : null,
                    this.OutputRemovedImg ? "-OutputRemovedImg" : null,
                    this.EnableDarkMode ? "-EnableDarkMode" : null,
                    "-PngComparison " + this.Comparer.PngComparison,
                    this.Comparer.ResolvePngLink ? "-ResolvePngLink" : null,
                }.Where(p => p != null)));
                sw.WriteLine("</table>");
                sw.WriteLine("</p>");

                //输出目录
                StringBuilder[] sb = { new StringBuilder(), new StringBuilder(), new StringBuilder() };
                int[] count = new int[6];
                string[] diffStr = { "변경", "추가", "제거" };
                foreach (CompareDifference diff in diffLst)
                {
                    int idx = -1;
                    string detail = null;
                    switch (diff.DifferenceType)
                    {
                        case DifferenceType.Changed:
                            idx = 0;
                            detail = string.Format("<a name=\"m_{1}_{2}\" href=\"#a_{1}_{2}\">{0}</a>", diff.NodeNew.FullPathToFile, idx, count[idx]);
                            break;
                        case DifferenceType.Append:
                            idx = 1;
                            if (this.OutputAddedImg)
                            {
                                detail = string.Format("<a name=\"m_{1}_{2}\" href=\"#a_{1}_{2}\">{0}</a>", diff.NodeNew.FullPathToFile, idx, count[idx]);
                            }
                            else
                            {
                                detail = diff.NodeNew.FullPathToFile;
                            }
                            break;
                        case DifferenceType.Remove:
                            idx = 2;
                            if (this.OutputRemovedImg)
                            {
                                detail = string.Format("<a name=\"m_{1}_{2}\" href=\"#a_{1}_{2}\">{0}</a>", diff.NodeOld.FullPathToFile, idx, count[idx]);
                            }
                            else
                            {
                                detail = diff.NodeOld.FullPathToFile;
                            }
                            break;
                        default:
                            continue;
                    }
                    sb[idx].Append("<tr><td>");
                    sb[idx].Append(detail);
                    sb[idx].AppendLine("</td></tr>");
                    count[idx]++;
                }
                StateDetail = "목차 출력중";
                Array.Copy(count, 0, count, 3, 3);
                for (int i = 0; i < sb.Length; i++)
                {
                    sw.WriteLine("<table class=\"lst{0}\">", i);
                    sw.WriteLine("<tr><th><a name=\"m_{0}\">{1}:{2}</a></th></tr>", i, diffStr[i], count[i]);
                    sw.Write(sb[i].ToString());
                    sw.WriteLine("</table>");
                    sb[i] = null;
                    count[i] = 0;
                }

                Patcher.PatchPartContext part = new Patcher.PatchPartContext("", 0, 0);
                part.NewFileLength = count[3] + (this.OutputAddedImg ? count[4] : 0) + (this.OutputRemovedImg ? count[5] : 0);

                OnPatchingStateChanged(new Patcher.PatchingEventArgs(part, Patcher.PatchingState.CompareStarted));

                foreach (CompareDifference diff in diffLst)
                {
                    OnPatchingStateChanged(new Patcher.PatchingEventArgs(part, Patcher.PatchingState.TempFileBuildProcessChanged, count[0] + count[1] + count[2]));
                    switch (diff.DifferenceType)
                    {
                        case DifferenceType.Changed:
                            {
                                StateInfo = string.Format("{0}/{1} 변경: {2}", count[0], count[3], diff.NodeNew.FullPath);
                                Wz_Image imgNew, imgOld;
                                if ((imgNew = diff.ValueNew as Wz_Image) != null
                                    && ((imgOld = diff.ValueOld as Wz_Image) != null))
                                {
                                    string anchorName = "a_0_" + count[0];
                                    string menuAnchorName = "m_0_" + count[0];
                                    CompareImg(imgNew, imgOld, diff.NodeNew.FullPathToFile, anchorName, menuAnchorName, srcDirPath, sw);
                                }
                                count[0]++;
                            }
                            break;

                        case DifferenceType.Append:
                            if (this.OutputAddedImg)
                            {
                                StateInfo = string.Format("{0}/{1} 추가: {2}", count[1], count[4], diff.NodeNew.FullPath);
                                Wz_Image imgNew = diff.ValueNew as Wz_Image;
                                if (imgNew != null)
                                {
                                    string anchorName = "a_1_" + count[1];
                                    string menuAnchorName = "m_1_" + count[1];
                                    OutputImg(imgNew, diff.DifferenceType, diff.NodeNew.FullPathToFile, anchorName, menuAnchorName, srcDirPath, sw);
                                }
                                count[1]++;
                            }
                            break;

                        case DifferenceType.Remove:
                            if (this.OutputRemovedImg)
                            {
                                StateInfo = string.Format("{0}/{1} 제거: {2}", count[2], count[5], diff.NodeOld.FullPath);
                                Wz_Image imgOld = diff.ValueOld as Wz_Image;
                                if (imgOld != null)
                                {
                                    string anchorName = "a_2_" + count[2];
                                    string menuAnchorName = "m_2_" + count[2];
                                    OutputImg(imgOld, diff.DifferenceType, diff.NodeOld.FullPathToFile, anchorName, menuAnchorName, srcDirPath, sw);
                                }
                                count[2]++;
                            }
                            break;

                        case DifferenceType.NotChanged:
                            break;
                    }

                }
                //html结束
                sw.WriteLine("</body>");
                sw.WriteLine("</html>");

                if (index != null)
                {
                    index.WriteLine("<tr><td><a href=\"{0}.html\">{0}.wz</a></td><td>{1}</td><td>{2}</td><td><a href=\"{0}.html#m_0\">{3}</a></td><td><a href=\"{0}.html#m_1\">{4}</a></td><td><a href=\"{0}.html#m_2\">{5}</a></td></tr>",
                        type.ToString(),
                        string.Join("<br/>", fileNew.SelectMany(wzf => GetFileInfo(wzf, ewzf => ewzf.Header.FileSize.ToString("N0")))),
                        string.Join("<br/>", fileOld.SelectMany(wzf => GetFileInfo(wzf, ewzf => ewzf.Header.FileSize.ToString("N0")))),
                        count[3],
                        count[4],
                        count[5]
                        );
                    index.Flush();
                }
            }
            finally
            {
                try
                {
                    if (sw != null)
                    {
                        sw.Flush();
                        sw.Close();
                    }
                }
                catch
                {
                }
                OnPatchingStateChanged(new Patcher.PatchingEventArgs(null, Patcher.PatchingState.CompareFinished));
            }

            if (type.ToString() == "String")
            {
                if (OutputSkillTooltip && OutputSkillTooltipIDs != null)
                {
                    string tooltipPath = Path.Combine(outputDir, "스킬 툴팁");
                    if (!Directory.Exists(tooltipPath))
                    {
                        Directory.CreateDirectory(tooltipPath);
                    }
                    SaveSkillTooltip(tooltipPath);
                }
                if (OutputItemTooltip && OutputItemTooltipIDs != null)
                {
                    string tooltipPath = Path.Combine(outputDir, "아이템 툴팁");
                    if (!Directory.Exists(tooltipPath))
                    {
                        Directory.CreateDirectory(tooltipPath);
                    }
                    SaveItemTooltip(tooltipPath);
                }
                if (OutputGearTooltip && OutputGearTooltipIDs != null)
                {
                    string tooltipPath = Path.Combine(outputDir, "장비 툴팁");
                    if (!Directory.Exists(tooltipPath))
                    {
                        Directory.CreateDirectory(tooltipPath);
                    }
                    SaveGearTooltip(tooltipPath);
                }
                if (OutputMapTooltip && OutputMapTooltipIDs != null)
                {
                    string tooltipPath = Path.Combine(outputDir, "맵 툴팁");
                    if (!Directory.Exists(tooltipPath))
                    {
                        Directory.CreateDirectory(tooltipPath);
                    }
                    SaveMapTooltip(tooltipPath);
                }

                for (var i = 0; i < 2; i++)
                {
                    this.WzNewOld[i] = null;
                    this.WzFileNewOld[i] = null;
                    this.StringLinkerNewOld[i] = null;
                }
            }
        }

        // 변경된 스킬 툴팁 출력
        private void SaveSkillTooltip(string tooltipPath)
        {
            SkillTooltipRender2[] tooltipRenderNewOld = new SkillTooltipRender2[2];
            int count = 0;
            int allCount = OutputSkillTooltipIDs.Count;

            for (int i = 0; i < 2; i++) // 0: New, 1: Old
            {
                tooltipRenderNewOld[i] = new SkillTooltipRender2();
                tooltipRenderNewOld[i].StringLinker = this.StringLinkerNewOld[i];
                tooltipRenderNewOld[i].ShowObjectID = true;
                tooltipRenderNewOld[i].ShowDelay = true;
                tooltipRenderNewOld[i].ShowArea = true;
                tooltipRenderNewOld[i].SourceWzNode = WzNewOld[i];
                tooltipRenderNewOld[i].SourceWzFile = WzFileNewOld[i];
                tooltipRenderNewOld[i].DiffSkillTags = this.DiffSkillTags;
                tooltipRenderNewOld[i].IgnoreEvalError = true;
                tooltipRenderNewOld[i].Enable22AniStyle = CharaSimConfig.Default.Misc.Enable22AniStyle;
            }

            foreach (var skillID in OutputSkillTooltipIDs)
            {
                StateInfo = string.Format("{0}/{1} 스킬: {2}", ++count, allCount, skillID);
                StateDetail = "스킬 변경점을 툴팁 이미지로 출력중...";

                string nodePath = skillID / 10000000 == 8 ? $@"\{(skillID / 100):D3}.img\skill\{skillID:D7}"
                    : $@"\{(skillID / 10000):D3}.img\skill\{skillID:D7}";
                int nullIdx = 0;

                // 변경 전후 툴팁 이미지 생성
                for (int i = 0; i < 2; i++) // 0: New, 1: Old
                {
                    Skill skill = Skill.CreateFromNode(PluginManager.FindWz("Skill" + nodePath, WzFileNewOld[i]), PluginManager.FindWz, WzFileNewOld[i]) ??
                        (Skill.CreateFromNode(PluginManager.FindWz("Skill001" + nodePath, WzFileNewOld[i]), PluginManager.FindWz, WzFileNewOld[i]) ??
                        (Skill.CreateFromNode(PluginManager.FindWz("Skill002" + nodePath, WzFileNewOld[i]), PluginManager.FindWz, WzFileNewOld[i]) ??
                        Skill.CreateFromNode(PluginManager.FindWz("Skill003" + nodePath, WzFileNewOld[i]), PluginManager.FindWz, WzFileNewOld[i])));
                    
                    if (skill != null)
                    {
                        skill.Level = skill.MaxLevel;
                        tooltipRenderNewOld[i].Skill = skill;
                    }
                    else
                    {
                        nullIdx |= i + 1;
                        tooltipRenderNewOld[i].Skill = null;
                    }
                }

                SaveTooltip(tooltipRenderNewOld[0], tooltipRenderNewOld[1], nullIdx, tooltipPath, skillID, "스킬");
            }
            OutputSkillTooltipIDs.Clear();
            DiffSkillTags.Clear();
        }

        // 변경된 아이템 툴팁 출력
        private void SaveItemTooltip(string tooltipPath)
        {
            ItemTooltipRender2[] tooltipRenderNewOld = new ItemTooltipRender2[2];
            int count = 0;
            int allCount = OutputItemTooltipIDs.Count;

            for (int i = 0; i < 2; i++) // 0: New, 1: Old
            {
                tooltipRenderNewOld[i] = new ItemTooltipRender2();
                tooltipRenderNewOld[i].StringLinker = this.StringLinkerNewOld[i];
                tooltipRenderNewOld[i].ShowObjectID = true;
                tooltipRenderNewOld[i].LinkRecipeInfo = true;
                tooltipRenderNewOld[i].LinkRecipeItem = true;
                tooltipRenderNewOld[i].ShowLevelOrSealed = true;
                tooltipRenderNewOld[i].ShowNickTag = true;
                tooltipRenderNewOld[i].ShowNickTag = true;
                tooltipRenderNewOld[i].SourceWzFile = WzFileNewOld[i];
                tooltipRenderNewOld[i].CosmeticHairColor = CharaSimConfig.Default.Item.CosmeticHairColor;
                tooltipRenderNewOld[i].CosmeticFaceColor = CharaSimConfig.Default.Item.CosmeticFaceColor;
                tooltipRenderNewOld[i].Enable22AniStyle = CharaSimConfig.Default.Misc.Enable22AniStyle;
            }

            foreach (var itemID in OutputItemTooltipIDs)
            {
                StateInfo = string.Format("{0}/{1} 아이템: {2}", ++count, allCount, itemID);
                StateDetail = "아이템 변경점을 툴팁 이미지로 출력중...";

                string itemType = Item.GetItemType(itemID).ToString();
                string nodePath = (itemID / 1000000 == 5) ? $@"\{itemID:D8}.img\"
                    : (itemID / 100 == 3015) ? $@"\{(itemID / 100):D6}.img\{itemID:D8}"
                    : (itemID / 1000 == 301) ? $@"\{(itemID / 1000):D5}.img\{itemID:D8}"
                    : $@"\{(itemID / 10000):D4}.img\{itemID:D8}";
                int nullIdx = 0;

                // 변경 전후 툴팁 이미지 생성
                for (int i = 0; i < 2; i++) // 0: New, 1: Old
                {
                    Item item = Item.CreateFromNode(PluginManager.FindWz($@"Item\{itemType}{nodePath}", WzFileNewOld[i]), PluginManager.FindWz, WzFileNewOld[i]);

                    if (item != null)
                    {
                        tooltipRenderNewOld[i].Item = item;
                    }
                    else
                    {
                        nullIdx |= i + 1;
                        tooltipRenderNewOld[i].Item = null;
                    }
                }

                SaveTooltip(tooltipRenderNewOld[0], tooltipRenderNewOld[1], nullIdx, tooltipPath, itemID, "아이템");
            }
            OutputItemTooltipIDs.Clear();
        }

        // 변경된 장비 툴팁 출력
        private void SaveGearTooltip(string tooltipPath)
        {
            TooltipRender[] tooltipRenderNewOld = new TooltipRender[2];
            int count = 0;
            int allCount = OutputGearTooltipIDs.Count;

            for (int i = 0; i < 2; i++) // 0: New, 1: Old
            {
                if (CharaSimConfig.Default.Misc.Enable22AniStyle)
                {
                    tooltipRenderNewOld[i] = new GearTooltipRender22();
                    tooltipRenderNewOld[i].StringLinker = this.StringLinkerNewOld[i];
                    tooltipRenderNewOld[i].ShowObjectID = true;
                    tooltipRenderNewOld[i].SourceWzFile = WzFileNewOld[i];
                    (tooltipRenderNewOld[i] as GearTooltipRender22).ShowLevelOrSealed = true;
                    (tooltipRenderNewOld[i] as GearTooltipRender22).MaxStar25 = CharaSimConfig.Default.Gear.MaxStar25;
                }
                else
                {
                    tooltipRenderNewOld[i] = new GearTooltipRender2();
                    tooltipRenderNewOld[i].StringLinker = this.StringLinkerNewOld[i];
                    tooltipRenderNewOld[i].ShowObjectID = true;
                    tooltipRenderNewOld[i].SourceWzFile = WzFileNewOld[i];
                    (tooltipRenderNewOld[i] as GearTooltipRender2).ShowLevelOrSealed = true;
                    (tooltipRenderNewOld[i] as GearTooltipRender2).MaxStar25 = CharaSimConfig.Default.Gear.MaxStar25;
                }
            }

            foreach (var gearID in OutputGearTooltipIDs)
            {
                StateInfo = string.Format("{0}/{1} 장비: {2}", ++count, allCount, gearID);
                StateDetail = "장비 변경점을 툴팁 이미지로 출력중...";

                string gearType = Gear.GetGearDirName(gearID);
                if (!string.IsNullOrEmpty(gearType))
                {
                    gearType = @"\" + gearType;
                }
                string nodePath = $@"\{gearID:D8}.img";
                int nullIdx = 0;

                // 변경 전후 툴팁 이미지 생성
                for (int i = 0; i < 2; i++) // 0: New, 1: Old
                {
                    Gear gear = Gear.CreateFromNode(PluginManager.FindWz($@"Character{gearType}{nodePath}", WzFileNewOld[i]), PluginManager.FindWz, WzFileNewOld[i]);

                    if (gear != null)
                    {
                        if (tooltipRenderNewOld[i] is GearTooltipRender22)
                            (tooltipRenderNewOld[i] as GearTooltipRender22).Gear = gear;
                        else
                            (tooltipRenderNewOld[i] as GearTooltipRender2).Gear = gear;
                    }
                    else
                    {
                        nullIdx |= i + 1;
                        if (tooltipRenderNewOld[i] is GearTooltipRender22)
                            (tooltipRenderNewOld[i] as GearTooltipRender22).Gear = null;
                        else
                            (tooltipRenderNewOld[i] as GearTooltipRender2).Gear = null;
                    }
                }

                SaveTooltip(tooltipRenderNewOld[0], tooltipRenderNewOld[1], nullIdx, tooltipPath, gearID, "장비");
            }
            OutputGearTooltipIDs.Clear();
        }

        // 변경된 맵 툴팁 출력
        private void SaveMapTooltip(string tooltipPath)
        {
            MapTooltipRenderer[] tooltipRenderNewOld = new MapTooltipRenderer[2];
            int count = 0;
            int allCount = OutputMapTooltipIDs.Count;

            for (int i = 0; i < 2; i++) // 0: New, 1: Old
            {
                tooltipRenderNewOld[i] = new MapTooltipRenderer();
                tooltipRenderNewOld[i].StringLinker = this.StringLinkerNewOld[i];
                tooltipRenderNewOld[i].ShowObjectID = true;
                tooltipRenderNewOld[i].ShowMiniMap = true;
                tooltipRenderNewOld[i].SourceWzFile = WzFileNewOld[i];
            }

            foreach (var mapID in OutputMapTooltipIDs)
            {
                StateInfo = string.Format("{0}/{1} 맵: {2}", ++count, allCount, mapID);
                StateDetail = "맵 변경점을 툴팁 이미지로 출력중...";

                string nodePath = $@"\{mapID:D9}.img";
                int nullIdx = 0;

                // 변경 전후 툴팁 이미지 생성
                for (int i = 0; i < 2; i++) // 0: New, 1: Old
                {
                    Map map = Map.CreateFromNode(PluginManager.FindWz($@"Map\Map\Map{mapID / 100000000}{nodePath}", WzFileNewOld[i]), PluginManager.FindWz, WzFileNewOld[i]);

                    if (map != null)
                    {
                        tooltipRenderNewOld[i].Map = map;
                    }
                    else
                    {
                        nullIdx |= i + 1;
                        tooltipRenderNewOld[i].Map = null;
                    }
                }

                SaveTooltip(tooltipRenderNewOld[0], tooltipRenderNewOld[1], nullIdx, tooltipPath, mapID, "맵", typePicH: 1);
            }
            OutputGearTooltipIDs.Clear();
        }

        private void SaveTooltip(TooltipRender RenderNew, TooltipRender RenderOld, int nullIdx, string tooltipPath, int ID, string tooltipType, int typePicH = 13)
        {
            // 툴팁 이미지 합치기
            Bitmap resultImage = null;
            Graphics g = null;
            string type = "";

            switch (nullIdx)
            {
                case 0: // change
                    type = "변경";
                    Bitmap ImageNew = null;
                    Bitmap ImageOld = null;
                    if (RenderNew is SkillTooltipRender2)
                    {
                        ImageNew = (RenderNew as SkillTooltipRender2).Render(true);
                        ImageOld = (RenderOld as SkillTooltipRender2).Render(true);
                    }
                    else
                    {
                        ImageNew = RenderNew.Render();
                        ImageOld = RenderOld.Render();
                    }
                    resultImage = new Bitmap(ImageNew.Width + ImageOld.Width, Math.Max(ImageNew.Height, ImageOld.Height));
                    g = Graphics.FromImage(resultImage);

                    g.DrawImage(ImageOld, 0, 0);
                    g.DrawImage(ImageNew, ImageOld.Width, 0);
                    ImageNew.Dispose();
                    ImageOld.Dispose();
                    break;

                case 1: // delete
                    type = "삭제";

                    resultImage = RenderOld.Render();
                    g = Graphics.FromImage(resultImage);
                    break;

                case 2: // add
                    type = "추가";

                    resultImage = RenderNew.Render();
                    g = Graphics.FromImage(resultImage);
                    break;

                default:
                    break;
            }

            if (resultImage == null || g == null)
            {
                return;
            }

            int picH = typePicH;
            GearGraphics.DrawPlainText(g, type, GearGraphics.EquipMDMoris9Font, Color.FromArgb(255, 255, 255), 2, 100, ref picH, 10);

            string add = tooltipType == "스킬" ? $"[{(ItemStringHelper.GetJobName(ID / 10000) ?? "기타")}]"
                : tooltipType == "장비" ? $"[{(ItemStringHelper.GetGearTypeString(Gear.GetGearType(ID)) ?? (ID / 10000 == 170 ? "무기" : "기타"))}]"
                : tooltipType == "아이템" ? $"[{ItemStringHelper.GetItemCategoryName(Item.GetItemType(ID)) ?? "?"}]" : "";
            string imageName = Path.Combine(tooltipPath, $"{tooltipType}_{ID}{add}_{type}.png");
            if (!File.Exists(imageName))
            {
                resultImage.Save(imageName, System.Drawing.Imaging.ImageFormat.Png);
            }
            resultImage.Dispose();
            g.Dispose();
        }

        // 노드에서 스킬 ID 얻기
        private void GetSkillID(Wz_Node node, bool change)
        {
            if (node == null) return;

            Match match = Regex.Match(node.FullPathToFile, @"^String\\Skill.img\\(\d+).*");
            string tag = null;

            if (!match.Success)
            {
                tag = node.Text;
                match = Regex.Match(node.FullPathToFile, @"^Skill\d*\\\d+.img\\skill\\(\d+)\\(common|masterLevel|combatOrders|action|isPetAutoBuff|isSequenceOn|BGM).*"); // 변경점 중 스킬 툴팁 출력할 것들

                if (change && !match.Success)
                {
                    match = Regex.Match(node.FullPathToFile, @"^Skill\\_Canvas\\\d+.img\\skill\\(\d+)\\(icon)$"); // 스킬 아이콘 변경 체크
                }
            }

            if (match.Success)
            {
                string skillID = match.Groups[1].ToString();

                if (skillID != null)
                {
                    if (!OutputSkillTooltipIDs.Contains(int.Parse(skillID)))
                    {
                        OutputSkillTooltipIDs.Add(int.Parse(skillID));
                        DiffSkillTags[skillID] = new HashSet<string>();
                    }

                    if (tag != null && !DiffSkillTags[skillID].Contains(tag))
                    {
                        DiffSkillTags[skillID].Add(tag);
                    }
                }
            }
        }

        // 노드에서 아이템 ID 얻기
        private void GetItemID(Wz_Node node, bool change)
        {
            if (node == null) return;

            Match match = Regex.Match(node.FullPathToFile, @"^String\\(?:Cash|Consume|Etc|Ins|Pet).img\\(?:.+?\\)?(\d+).*");

            if (!match.Success)
            {
                if (!change)
                    match = Regex.Match(node.FullPathToFile, @"^Item\\(?:Cash|Consume|Etc|Install|Pet)\\\d+.img\\(\d+)\\info\\.*"); // 변경점 중 툴팁 출력할 것들

                if (change && !match.Success)
                {
                    match = Regex.Match(node.FullPathToFile, @"^Item\\(?:Cash|Consume|Etc|Install|Pet)\\_Canvas\\\d+.img\\(\d+)\\info\\(icon)$"); // 아이콘 변경 체크
                }
            }

            if (match.Success)
            {
                string itemID = match.Groups[1].ToString();

                if (itemID != null)
                {
                    if (!OutputItemTooltipIDs.Contains(int.Parse(itemID)))
                    {
                        OutputItemTooltipIDs.Add(int.Parse(itemID));
                    }
                }
            }
        }

        // 노드에서 장비 ID 얻기
        private void GetGearID(Wz_Node node, bool change)
        {
            if (node == null) return;

            Match match = Regex.Match(node.FullPathToFile, @"^String\\Eqp.img\\Eqp\\(?:.+?\\)?(\d+).*");

            if (!match.Success)
            {
                if (!change)
                    match = Regex.Match(node.FullPathToFile, @"^Character\\.+?\\(\d+).img\\info\\.*"); // 변경점 중 툴팁 출력할 것들

                if (change && !match.Success)
                {
                    match = Regex.Match(node.FullPathToFile, @"^Character\\.+?\\_Canvas\\(\d+).img\\info\\(icon)$"); // 아이콘 변경 체크
                }
            }

            if (match.Success)
            {
                string itemID = match.Groups[1].ToString();

                if (itemID != null)
                {
                    if (!OutputGearTooltipIDs.Contains(int.Parse(itemID)))
                    {
                        OutputGearTooltipIDs.Add(int.Parse(itemID));
                    }
                }
            }
        }

        // 노드에서 맵 ID 얻기
        private void GetMapID(Wz_Node node, bool change)
        {
            if (node == null) return;

            Match match = Regex.Match(node.FullPathToFile, @"^String\\Map.img\\.+?\\(\d+).*");

            if (!match.Success)
            {
                match = Regex.Match(node.FullPathToFile, @"^Map\\Map\\Map\d\\(\d+).img\\info\\(barrier|barrierArc|barrierAut).*"); // 변경점 중 툴팁 출력할 것들

                if (change && !match.Success)
                {
                    match = Regex.Match(node.FullPathToFile, @"^Map\\Map\\Map\d\\(\d+).img\\miniMap\\(canvas)$"); // 아이콘 변경 체크
                }
                else if (!change && !match.Success)
                {
                    match = Regex.Match(node.FullPathToFile, @"^Map\\Map\\Map\d\\(\d+).img\\info\\.*");
                }
            }

            if (!match.Success)
            {
                match = Regex.Match(node.FullPathToFile, @"^Etc\\MapObjectInfo.img\\(\d+)\\.*");
            }

            if (match.Success)
            {
                string itemID = match.Groups[1].ToString();

                if (itemID != null)
                {
                    if (!OutputMapTooltipIDs.Contains(int.Parse(itemID)))
                    {
                        OutputMapTooltipIDs.Add(int.Parse(itemID));
                    }
                }
            }
        }

        private void CompareImg(Wz_Image imgNew, Wz_Image imgOld, string imgName, string anchorName, string menuAnchorName, string outputDir, StreamWriter sw)
        {
            StateDetail = "img 구조 분석중";
            if (!imgNew.TryExtract() || !imgOld.TryExtract())
                return;
            StateDetail = "img 비교중";
            List<CompareDifference> diffList = new List<CompareDifference>(Comparer.Compare(imgNew.Node, imgOld.Node));
            StringBuilder sb = new StringBuilder();
            int[] count = new int[3];
            StateDetail = "총 " + diffList.Count + "개의 변경사항 발견, 합산중";
            foreach (var diff in diffList)
            {
                int idx = -1;
                string fullPath = null;
                string fullPathToFile = null;
                switch (diff.DifferenceType)
                {
                    case DifferenceType.Changed:
                        idx = 0;
                        fullPath = diff.NodeNew.FullPath;
                        fullPathToFile = diff.NodeNew.FullPathToFile;
                        break;
                    case DifferenceType.Append:
                        idx = 1;
                        fullPath = diff.NodeNew.FullPath;
                        fullPathToFile = diff.NodeNew.FullPathToFile;
                        break;
                    case DifferenceType.Remove:
                        idx = 2;
                        fullPath = diff.NodeOld.FullPath;
                        fullPathToFile = diff.NodeOld.FullPathToFile;
                        break;
                }
                sb.AppendFormat("<tr class=\"r{0}\">", idx);
                sb.AppendFormat("<td>{0}</td>", fullPath ?? " ");
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(fullPathToFile, diff.NodeNew, 0, outputDir) ?? " ");
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(fullPathToFile, diff.NodeOld, 1, outputDir) ?? " ");
                sb.AppendLine("</tr>");
                count[idx]++;

                // 변경된 툴팁 출력
                if (OutputSkillTooltip && (imgName.Contains("Skill") || imgName.Contains("String")))
                {
                    GetSkillID(diff.NodeNew, idx == 0 ? true : false);
                    GetSkillID(diff.NodeOld, idx == 0 ? true : false);
                }
                if (OutputItemTooltip && (imgName.Contains("Item") || imgName.Contains("String")))
                {
                    GetItemID(diff.NodeNew, idx == 0 ? true : false);
                    GetItemID(diff.NodeOld, idx == 0 ? true : false);
                }
                if (OutputGearTooltip && (imgName.Contains("Character") || imgName.Contains("String")))
                {
                    GetGearID(diff.NodeNew, idx == 0 ? true : false);
                    GetGearID(diff.NodeOld, idx == 0 ? true : false);
                }
                if (OutputMapTooltip && (imgName.Contains("Etc") || imgName.Contains("Map") || imgName.Contains("String")))
                {
                    GetMapID(diff.NodeNew, idx == 0 ? true : false);
                    GetMapID(diff.NodeOld, idx == 0 ? true : false);
                }
            }
            StateDetail = "문서 출력중";
            bool noChange = diffList.Count <= 0;
            sw.WriteLine("<table class=\"img{0}\">", noChange ? " noChange" : "");
            sw.WriteLine("<tr><th colspan=\"3\"><a name=\"{1}\">{0}</a> 변경:{2} 추가:{3} 제거:{4}</th></tr>",
                imgName, anchorName, count[0], count[1], count[2]);
            sw.WriteLine(sb.ToString());
            sw.WriteLine("<tr><td colspan=\"3\"><a href=\"#{1}\">{0}</a></td></tr>", "돌아가기", menuAnchorName);
            sw.WriteLine("</table>");
            imgNew.Unextract();
            imgOld.Unextract();
            sb = null;
        }

        private void OutputImg(Wz_Image img, DifferenceType diffType, string imgName, string anchorName, string menuAnchorName, string outputDir, StreamWriter sw)
        {
            StateDetail = "img 구조 분석중";
            if (!img.TryExtract())
                return;

            int idx = 0; ;
            switch (diffType)
            {
                case DifferenceType.Changed:
                    idx = 0;
                    break;
                case DifferenceType.Append:
                    idx = 1;
                    break;
                case DifferenceType.Remove:
                    idx = 2;
                    break;
            }
            Action<Wz_Node> fnOutput = null;
            fnOutput = node =>
            {
                if (node != null)
                {
                    string fullPath = node.FullPath;
                    string fullPathToFile = node.FullPathToFile;
                    sw.Write("<tr class=\"r{0}\">", idx);
                    sw.Write("<td>{0}</td>", fullPath ?? " ");
                    sw.Write("<td>{0}</td>", OutputNodeValue(fullPathToFile, node, 0, outputDir) ?? " ");
                    sw.WriteLine("</tr>");

                    // 변경된 툴팁 출력
                    if (OutputSkillTooltip && (imgName.Contains("Skill") || imgName.Contains("String")))
                    {
                        GetSkillID(node, idx == 0 ? true : false);
                    }
                    if (OutputItemTooltip && (imgName.Contains("Item") || imgName.Contains("String")))
                    {
                        GetItemID(node, idx == 0 ? true : false);
                    }
                    if (OutputGearTooltip && (imgName.Contains("Character") || imgName.Contains("String")))
                    {
                        GetGearID(node, idx == 0 ? true : false);
                    }
                    if (OutputMapTooltip && (imgName.Contains("Etc") || imgName.Contains("Map") || imgName.Contains("String")))
                    {
                        GetMapID(node, idx == 0 ? true : false);
                    }

                    if (node.Nodes.Count > 0)
                    {
                        foreach (Wz_Node child in node.Nodes)
                        {
                            fnOutput(child);
                        }
                    }
                }
            };

            StateDetail = "img 구조 출력중";
            sw.WriteLine("<table class=\"img\">");
            sw.WriteLine("<tr><th colspan=\"2\"><a name=\"{1}\">{0}</a></th></tr>", imgName, anchorName);
            fnOutput(img.Node);
            sw.WriteLine("<tr><td colspan=\"2\"><a href=\"#{1}\">{0}</a></td></tr>", "돌아가기", menuAnchorName);
            sw.WriteLine("</table>");
            img.Unextract();
        }

        protected virtual string OutputNodeValue(string fullPath, Wz_Node value, int col, string outputDir)
        {
            if (value == null)
                return null;

            Wz_Node linkNode;
            if ((linkNode = value.GetLinkedSourceNode(path => PluginBase.PluginManager.FindWz(path, value.GetNodeWzFile()))) != value)
            {
                return "(link) " + OutputNodeValue(fullPath, linkNode, col, outputDir);
            }

            switch (value.Value)
            {
                case Wz_Png png:
                    if (OutputPng)
                    {
                        char[] invalidChars = Path.GetInvalidFileNameChars();
                        string colName = col == 0 ? "new" : (col == 1 ? "old" : col.ToString());
                        string fileName = fullPath.Replace('\\', '.');
                        string suffix = "_" + colName + ".png";
                        string canvas = "_Canvas";

                        if (this.HashPngFileName)
                        {
                            fileName = ToHexString(MD5Hash(fileName));
                            // TODO: save file name mapping to another file? 
                        }
                        else if (Environment.OSVersion.Platform == PlatformID.Win32NT && fileName.Length + suffix.Length > 255)
                        {
                            // force hashing if the file name too long.
                            // TODO: also need to check full file path, we have tested that all existing browsers on Windows cannot load
                            //       local files with excessively long path.
                            fileName = ToHexString(MD5Hash(fileName));
                        }
                        else
                        {
                            for (int i = 0; i < invalidChars.Length; i++)
                            {
                                fileName = fileName.Replace(invalidChars[i], '_');
                            }
                        }

                        fileName = fileName + suffix;
                        string outputDirName = new DirectoryInfo(outputDir).Name;
                        bool isCanvas = fileName.Contains(canvas);
                        if (isCanvas)
                        {
                            outputDir = Path.Combine(outputDir, canvas);
                            if (!Directory.Exists(outputDir))
                            {
                                Directory.CreateDirectory(outputDir);
                            }
                        }
                        using (Bitmap bmp = png.ExtractPng())
                        {
                            bmp.Save(Path.Combine(outputDir, fileName), System.Drawing.Imaging.ImageFormat.Png);
                        }
                        return string.Format("<img src=\"{0}/{1}\" />", isCanvas ? Path.Combine(outputDirName, canvas) : outputDirName, WebUtility.UrlEncode(fileName));
                    }
                    else
                    {
                        return string.Format("PNG {0}*{1} ({2}B)", png.Width, png.Height, png.DataLength);
                    }

                case Wz_Uol uol:
                    return "(uol) " + uol.Uol;

                case Wz_Vector vector:
                    return string.Format("({0}, {1})", vector.X, vector.Y);

                case Wz_Sound sound:
                    if (OutputPng)
                    {
                        char[] invalidChars = Path.GetInvalidFileNameChars();
                        string colName = col == 0 ? "new" : (col == 1 ? "old" : col.ToString());
                        string filePath = fullPath.Replace('\\', '.') + "_" + colName + ".mp3";

                        for (int i = 0; i < invalidChars.Length; i++)
                        {
                            filePath = filePath.Replace(invalidChars[i].ToString(), null);
                        }

                        byte[] mp3 = sound.ExtractSound();
                        if (mp3 != null)
                        {
                            FileStream fileStream = new FileStream(Path.Combine(outputDir, filePath), FileMode.Create, FileAccess.Write);
                            fileStream.Write(mp3, 0, mp3.Length);
                            fileStream.Close();
                        }
                        return string.Format("<audio controls src=\"{0}\" type=\"audio/mpeg\">오디오 {1}ms\n</audio>", Path.Combine(new DirectoryInfo(outputDir).Name, filePath), sound.Ms);
                    }
                    else
                    {
                        return string.Format("오디오 {0}ms", sound.Ms);
                    }

                case Wz_Convex convex:
                    return string.Format("convex {0}", string.Join(" ", convex.Points.Select(vec => $"({vec.X},{vec.Y})")));

                case Wz_RawData rawData:
                    return string.Format("rawdata {0} bytes", rawData.Length);

                case Wz_Video video:
                    return string.Format("video {0} bytes", video.Length);

                case Wz_Image _:
                    return "(img)";

                default:
                    return string.Format("<span title=\"{0}\">{1}</span>", value.Value?.GetType().Name, WebUtility.HtmlEncode(Convert.ToString(value.Value)));
            }
        }

        public virtual void CreateStyleSheet(string outputDir)
        {
            string path = Path.Combine(outputDir, "style.css");
            if (File.Exists(path))
                return;
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            if (EnableDarkMode)
            {

                sw.WriteLine("body { font-size:12px; background-color:black; color:white; }");
                sw.WriteLine("a { color:white; }");
                sw.WriteLine("p.wzf { }");
                sw.WriteLine("table, tr, th, td { border:1px solid #ff8000; border-collapse:collapse; }");
                sw.WriteLine("table { margin-bottom:16px; }");
                sw.WriteLine("th { text-align:left; }");
                sw.WriteLine("table.lst0 { }");
                sw.WriteLine("table.lst1 { }");
                sw.WriteLine("table.lst2 { }");
                sw.WriteLine("table.img { }");
                sw.WriteLine("table.img tr.r0 { background-color:#003049; }");
                sw.WriteLine("table.img tr.r1 { background-color:#000000; }");
                sw.WriteLine("table.img tr.r2 { background-color:#462306; }");
                sw.WriteLine("table.img.noChange { display:none; }");
            }
            else
            {
                sw.WriteLine("body { font-size:12px; }");
                sw.WriteLine("p.wzf { }");
                sw.WriteLine("table, tr, th, td { border:1px solid #ff8000; border-collapse:collapse; }");
                sw.WriteLine("table { margin-bottom:16px; }");
                sw.WriteLine("th { text-align:left; }");
                sw.WriteLine("table.lst0 { }");
                sw.WriteLine("table.lst1 { }");
                sw.WriteLine("table.lst2 { }");
                sw.WriteLine("table.img { }");
                sw.WriteLine("table.img tr.r0 { background-color:#fff4c4; }");
                sw.WriteLine("table.img tr.r1 { background-color:#ebf2f8; }");
                sw.WriteLine("table.img tr.r2 { background-color:#ffffff; }");
                sw.WriteLine("table.img.noChange { display:none; }");
            }
            sw.Flush();
            sw.Close();
        }

        private static byte[] MD5Hash(string text)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            }
        }

        private static string ToHexString(byte[] inArray)
        {
            StringBuilder hex = new StringBuilder(inArray.Length * 2);
            foreach (byte b in inArray)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}
