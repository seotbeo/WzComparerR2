﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Drawing;
using System.Linq;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.CharaSimControl;
using WzComparerR2.CharaSim;
using System.Text.RegularExpressions;

namespace WzComparerR2.Comparer
{
    public class EasyComparer
    {
        public EasyComparer()
        {
            this.Comparer = new WzFileComparer();
        }
        private Wz_Node wzNew { get; set; }
        private Wz_Node wzOld { get; set; }
        private Wz_File stringWzNew { get; set; }
        private Wz_File itemWzNew { get; set; }
        private Wz_File etcWzNew { get; set; }
        private Wz_File stringWzOld { get; set; }
        private Wz_File itemWzOld { get; set; }
        private Wz_File etcWzOld { get; set; }
        private List<string> TooltipInfo = new List<string>();
        private Dictionary<string, List<string>> diffSkillTags = new Dictionary<string, List<string>>();
        public WzFileComparer Comparer { get; protected set; }
        private string stateInfo;
        private string stateDetail;
        public bool OutputPng { get; set; }
        public bool OutputAddedImg { get; set; }
        public bool OutputRemovedImg { get; set; }
        public bool EnableDarkMode { get; set; }
        public bool saveSkillTooltip { get; set; }
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

                this.wzNew = fileNew.Node;
                this.wzOld = fileOld.Node;
                this.stringWzNew = wzNew?.FindNodeByPath("String").GetNodeWzFile();
                this.itemWzNew = wzNew?.FindNodeByPath("Item").GetNodeWzFile();
                this.etcWzNew = wzNew?.FindNodeByPath("Etc").GetNodeWzFile();
                this.stringWzOld = wzOld?.FindNodeByPath("String").GetNodeWzFile();
                this.itemWzOld = wzOld?.FindNodeByPath("Item").GetNodeWzFile();
                this.etcWzOld = wzOld?.FindNodeByPath("Etc").GetNodeWzFile();
                
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
            string skillTooltipPath = Path.Combine(outputDir, "스킬 툴팁");
            if (saveSkillTooltip && !Directory.Exists(skillTooltipPath))
            {
                Directory.CreateDirectory(skillTooltipPath);
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
            if (saveSkillTooltip && type.ToString() =="String" && TooltipInfo != null)
            {
                saveTooltip(skillTooltipPath);
            }
        }

        // 변경된 스킬 툴팁 출력
        private void saveTooltip(string skillTooltipPath)
        {
            StringLinker slNew = new StringLinker();
            StringLinker slOld = new StringLinker();
            SkillTooltipRender2 skillRenderNew = new SkillTooltipRender2();
            SkillTooltipRender2 skillRenderOld = new SkillTooltipRender2();
            int count = 0;
            int allCount = TooltipInfo.Count;

            slNew.Load(stringWzNew, itemWzNew, etcWzNew);
            slOld.Load(stringWzOld, itemWzOld, etcWzOld);
            skillRenderNew.StringLinker = slNew;
            skillRenderOld.StringLinker = slOld;
            skillRenderNew.ShowObjectID = true;
            skillRenderOld.ShowObjectID = true;
            skillRenderNew.DoSetDiffColor = true;
            skillRenderOld.DoSetDiffColor = true;
            skillRenderNew.diffSkillTags = this.diffSkillTags;
            skillRenderOld.diffSkillTags = this.diffSkillTags;

            foreach (var skillID in TooltipInfo)
            {
                count++;
                StateInfo = string.Format("{0}/{1} 스킬: {2}", count, allCount, skillID);
                StateDetail = "Skill 변경점을 툴팁 이미지로 출력중...";

                Bitmap skillImageNew = null;
                Bitmap skillImageOld = null;
                string skillType = "_삭제";
                string skillNodePath = int.Parse(skillID) / 10000000 == 8 ? String.Format(@"\{0:D}.img\skill\{1:D}", int.Parse(skillID) / 100, skillID) : String.Format(@"\{0:D}.img\skill\{1:D}", int.Parse(skillID) / 10000, skillID);
                if (int.Parse(skillID) / 10000 == 0) skillNodePath = String.Format(@"\000.img\skill\{0:D7}", skillID);
                int heightNew = 0, heightOld = 0;
                int width = 0;

                // 변경 후 툴팁 이미지 생성
                Skill skillNew = Skill.CreateFromNode(PluginManager.FindWz("Skill" + skillNodePath, wzNew.GetNodeWzFile()), PluginManager.FindWz) ??
                    (Skill.CreateFromNode(PluginManager.FindWz("Skill001" + skillNodePath, wzNew.GetNodeWzFile()), PluginManager.FindWz) ??
                    (Skill.CreateFromNode(PluginManager.FindWz("Skill002" + skillNodePath, wzNew.GetNodeWzFile()), PluginManager.FindWz) ??
                    Skill.CreateFromNode(PluginManager.FindWz("Skill003" + skillNodePath, wzNew.GetNodeWzFile()), PluginManager.FindWz)));
                if (skillNew != null)
                {
                    skillNew.Level = skillNew.MaxLevel;
                    skillRenderNew.Skill = skillNew;
                    skillImageNew = skillRenderNew.Render();
                    width += skillImageNew.Width;
                    heightNew = skillImageNew.Height;
                }
                // 변경 전 툴팁 이미지 생성
                Skill skillOld = Skill.CreateFromNode(PluginManager.FindWz("Skill" + skillNodePath, wzOld.GetNodeWzFile()), PluginManager.FindWz) ??
                    (Skill.CreateFromNode(PluginManager.FindWz("Skill001" + skillNodePath, wzOld.GetNodeWzFile()), PluginManager.FindWz) ??
                    (Skill.CreateFromNode(PluginManager.FindWz("Skill002" + skillNodePath, wzOld.GetNodeWzFile()), PluginManager.FindWz) ??
                    Skill.CreateFromNode(PluginManager.FindWz("Skill003" + skillNodePath, wzOld.GetNodeWzFile()), PluginManager.FindWz)));
                if (skillOld != null)
                {
                    skillOld.Level = skillOld.MaxLevel;
                    skillRenderOld.Skill = skillOld;
                    skillImageOld = skillRenderOld.Render();
                    width += skillImageOld.Width;
                    heightOld = skillImageOld.Height;
                }
                if (width == 0) continue;
                // 툴팁 이미지 합치기
                Bitmap resultImage = new Bitmap(width, Math.Max(heightNew, heightOld));
                Graphics g = Graphics.FromImage(resultImage);

                if (skillImageOld != null)
                {
                    if (skillImageNew != null)
                    {
                        g.DrawImage(skillImageNew, skillImageOld.Width, 0);
                        skillImageNew.Dispose();
                        skillType = "_변경";
                    }
                    g.DrawImage(skillImageOld, 0, 0);
                    skillImageOld.Dispose();
                }
                else
                {
                    g.DrawImage(skillImageNew, 0, 0);
                    skillImageNew.Dispose();
                    skillType = "_추가";
                }

                string imageName = Path.Combine(skillTooltipPath, "스킬_" + skillID + '[' + (ItemStringHelper.GetJobName(int.Parse(skillID) / 10000) ?? "기타") + ']' + skillType + ".png");
                if (!File.Exists(imageName))
                {
                    resultImage.Save(Path.Combine(skillTooltipPath, "스킬_" + skillID + '[' + (ItemStringHelper.GetJobName(int.Parse(skillID) / 10000) ?? "기타") + ']' + skillType + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                resultImage.Dispose();
                g.Dispose();
            }
            TooltipInfo.Clear();
            diffSkillTags.Clear();
        }

        // Skill diff 노드에서 스킬 ID 얻기
        private void getIDFromSkill(Wz_Node node)
        {
            var tag = node.Text;
            Match match = Regex.Match(node.FullPathToFile, @"^Skill\d*\\\d+.img\\skill\\(\d+)\\(common|masterLevel|combatOrders).*");
            if (match.Success)
            {
                string skillID = match.Groups[1].ToString();
                if (!TooltipInfo.Contains(skillID) && skillID != null)
                {
                    TooltipInfo.Add(skillID);
                    diffSkillTags[skillID] = new List<string>();
                    diffSkillTags[skillID].Add(tag);
                }
                else if (TooltipInfo.Contains(skillID) && skillID != null)
                {
                    if (!diffSkillTags[skillID].Contains(tag))
                    {
                        diffSkillTags[skillID].Add(tag);
                    }
                }
            }
        }

        // String diff 노드에서 스킬 ID 얻기
        private void getIDFromString(Wz_Node node)
        {
            Match match = Regex.Match(node.FullPathToFile, @"^String\\Skill.img\\(\d+).*");
            if (match.Success)
            {
                string skillID = match.Groups[1].ToString();
                if (!TooltipInfo.Contains(skillID) && skillID != null)
                {
                    TooltipInfo.Add(skillID);
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
                string col0 = null;
                switch (diff.DifferenceType)
                {
                    case DifferenceType.Changed:
                        idx = 0;
                        col0 = diff.NodeNew.FullPath;
                        break;
                    case DifferenceType.Append:
                        idx = 1;
                        col0 = diff.NodeNew.FullPath;
                        break;
                    case DifferenceType.Remove:
                        idx = 2;
                        col0 = diff.NodeOld.FullPath;
                        break;
                }
                sb.AppendFormat("<tr class=\"r{0}\">", idx);
                sb.AppendFormat("<td>{0}</td>", col0 ?? " ");
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(col0, diff.NodeNew, 0, outputDir) ?? " ");
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(col0, diff.NodeOld, 1, outputDir) ?? " ");
                sb.AppendLine("</tr>");
                count[idx]++;

                // 변경된 스킬 툴팁 출력
                if (saveSkillTooltip && outputDir.Contains("Skill"))
                {
                    if (diff.NodeNew != null)
                    {
                        getIDFromSkill(diff.NodeNew);
                    }
                    if (diff.NodeOld != null)
                    {
                        getIDFromSkill(diff.NodeOld);
                    }
                }
                if (saveSkillTooltip && outputDir.Contains("String"))
                {
                    if (diff.NodeNew != null)
                    {
                        getIDFromString(diff.NodeNew);
                    }
                    if (diff.NodeOld != null)
                    {
                        getIDFromString(diff.NodeOld);
                    }
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
                    sw.Write("<tr class=\"r{0}\">", idx);
                    sw.Write("<td>{0}</td>", fullPath ?? " ");
                    sw.Write("<td>{0}</td>", OutputNodeValue(fullPath, node, 0, outputDir) ?? " ");
                    sw.WriteLine("</tr>");

                    if (saveSkillTooltip && outputDir.Contains("Skill")) // 변경된 스킬 툴팁 출력
                    {
                        getIDFromSkill(node);
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
            Wz_Png png;
            Wz_Uol uol;
            Wz_Sound sound;
            Wz_Vector vector;
            
            if ((linkNode = value.GetLinkedSourceNode(path => PluginBase.PluginManager.FindWz(path, value.GetNodeWzFile()))) != value)
            {
                return "(link) " + OutputNodeValue(fullPath, linkNode, col, outputDir);
            }
            else if ((png = value.Value as Wz_Png) != null)
            {
                if (OutputPng)
                {
                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    string colName = col == 0 ? "new" : (col == 1 ? "old" : col.ToString());
                    string filePath = fullPath.Replace('\\', '.') + "_" + colName + ".png";

                    for (int i = 0; i < invalidChars.Length; i++)
                    {
                        filePath = filePath.Replace(invalidChars[i].ToString(), null);
                    }

                    Bitmap bmp = png.ExtractPng();
                    if (bmp != null)
                    {
                        bmp.Save(Path.Combine(outputDir, filePath), System.Drawing.Imaging.ImageFormat.Png);
                        bmp.Dispose();
                    }
                    return string.Format("<img src=\"{0}/{1}\" />", new DirectoryInfo(outputDir).Name, WebUtility.UrlEncode(filePath));
                }
                else
                {
                    return string.Format("PNG {0}*{1} ({2}B)", png.Width, png.Height, png.DataLength);
                }

            }
            else if ((uol = value.Value as Wz_Uol) != null)
            {
                return "(uol) " + uol.Uol;
            }
            else if ((vector = value.Value as Wz_Vector) != null)
            {
                return string.Format("({0}, {1})", vector.X, vector.Y);
            }
            else if ((sound = value.Value as Wz_Sound) != null)
            {
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
            }
            else if (value.Value is Wz_Image)
            {
                return "(img)";
            }
            return Convert.ToString(value.Value);

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
    }
}
