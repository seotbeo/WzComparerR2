using System;
using System.Threading;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2.DB2
{
    /// <summary>
    /// 以反射方式啟動 MapRender 外掛。
    ///
    /// NTMS 版把地圖渲染器做成外掛，於獨立的 AssemblyLoadContext 載入，
    /// WzComparerR2.exe 不能直接參考 WzComparerR2.MapRender.FrmMapRender2；
    /// 而且它的建構式是無參數 + LoadMap(img)，和 WzComparerR2-Plus 的
    /// FrmMapRender2(img) 不同。外掛未安裝時所有方法均為無作用。
    /// </summary>
    internal static class MapRenderLauncher
    {
        private static Type gameType;
        private static bool searched;

        /// <summary>目前開啟中的地圖渲染器實體，未開啟時為 null。</summary>
        public static IDisposable Current { get; private set; }

        public static bool IsAvailable => ResolveType() != null;

        private static Type ResolveType()
        {
            if (!searched)
            {
                searched = true;
                // 外掛各自載入於獨立的 AssemblyLoadContext，
                // 但都屬於同一個 AppDomain，因此可由此列舉找到 MapRender。
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!asm.GetName().Name.Equals("WzComparerR2.MapRender", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var type = asm.GetType("WzComparerR2.MapRender.FrmMapRender2", false);
                    if (type != null)
                    {
                        gameType = type;
                        break;
                    }
                }
            }
            return gameType;
        }

        /// <summary>
        /// 顯示指定的地圖 img。已有開啟中的視窗時直接換圖，否則於 STA 執行緒開新視窗。
        /// </summary>
        public static void ShowMap(Wz_Node mapImgNode)
        {
            var type = ResolveType();
            if (type == null || mapImgNode == null)
            {
                return;
            }

            Wz_Image img = mapImgNode.Value as Wz_Image;
            if (img == null || !img.TryExtract())
            {
                return;
            }

            var running = Current;
            if (running != null)
            {
                try
                {
                    type.GetMethod("LoadMap", new[] { typeof(Wz_Image) })?.Invoke(running, new object[] { img });
                    return;
                }
                catch (Exception ex)
                {
                    PluginManager.LogError("MapRender", ex, "MapRender error:");
                    Current = null;
                }
            }

            var sl = new StringLinker();
            sl.Load(PluginManager.FindWz(Wz_Type.String).GetValueEx<Wz_File>(null),
                PluginManager.FindWz(Wz_Type.Item).GetValueEx<Wz_File>(null),
                PluginManager.FindWz(Wz_Type.Etc).GetValueEx<Wz_File>(null),
                PluginManager.FindWz(Wz_Type.Quest).GetValueEx<Wz_File>(null));

            var thread = new Thread(() =>
            {
                try
                {
                    var game = (IDisposable)Activator.CreateInstance(type);
                    type.GetProperty("StringLinker")?.SetValue(game, sl);
                    type.GetMethod("LoadMap", new[] { typeof(Wz_Image) })?.Invoke(game, new object[] { img });
                    Current = game;
                    try
                    {
                        using (game)
                        {
                            type.GetMethod("Run", Type.EmptyTypes)?.Invoke(game, null);
                        }
                    }
                    finally
                    {
                        Current = null;
                    }
                }
                catch (Exception ex)
                {
                    Current = null;
                    PluginManager.LogError("MapRender", ex, "MapRender error:");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>關閉目前開啟中的地圖渲染器視窗。</summary>
        public static void Close()
        {
            var game = Current;
            if (game != null)
            {
                Current = null;
                try
                {
                    game.Dispose();
                }
                catch (Exception ex)
                {
                    PluginManager.LogError("MapRender", ex, "MapRender error:");
                }
            }
        }
    }
}
