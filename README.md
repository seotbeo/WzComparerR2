# WzComparerR2-JMS
- これは、JMS 用に設計されたメイプルストーリー抽出ツールです。
- KMS、GMS、CMS などの他のクライアントで動作します。

# Modules
- **WzComparerR2** 主程序
- **WzComparerR2.Common** 一些通用类
- **WzComparerR2.PluginBase** 插件管理器
- **WzComparerR2.WzLib** wz文件读取相关
- **CharaSimResource** 用于装备模拟的资源文件
- **WzComparerR2.Updater** 程序更新器(未完成)
- **WzComparerR2.LuaConsole** (可选插件)Lua控制台
- **WzComparerR2.MapRender** (可选插件)地图仿真器
- **WzComparerR2.Avatar** (可选插件)纸娃娃
- **WzComparerR2.MonsterCard** (可选插件)怪物卡(已废弃)

# Usage
- **2.x**: Win7+/.net4.8+/dx11.0

# NX OpenAPI
この機能は KMS 内のリソースでのみ機能します。
[API キーを取得する方法については、こちらをご覧ください。](https://openapi.nexon.com/guide/prepare-in-advance/)他の国や地域のNX IDは使用できません。韓国のNX IDのみ使用できます。
[OpenAPI 機能の詳細については、こちらをご覧ください。](https://openapi.nexon.com/game/maplestory/)

### ItemID to NX OpenAPI ItemIcon Filename
|   |1st |2nd |3rd |4th |5th |6th |7th |
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|0  |    |P   |C   |L   |H   |O   |B   |
|1  |E   |O   |D   |A   |G   |P   |A   |
|2  |H   |N   |A   |J   |F   |M   |D   |
|3  |G   |M   |B   |I   |E   |N   |C   |
|4  |B   |L   |G   |P   |D   |K   |F   |
|5  |A   |K   |H   |O   |C   |L   |E   |
|6  |    |J   |E   |N   |B   |I   |H   |
|7  |    |I   |F   |M   |A   |J   |G   |
|8  |    |H   |K   |D   |P   |G   |J   |
|9  |    |G   |I   |C   |O   |H   |I   |

たとえば、次の ItemIcon URL はアイテム ID 1802767 を表します。
```
https://open.api.nexon.com/static/maplestory/ItemIcon/KEHCJAIG.png
```

# Compile
- vs2022 or higher/.net 6 SDK

# Credits
- **Fiel** ([Southperry](http://www.southperry.net))  wz文件读取代码改造自WzExtract 以及WzPatcher
- **Index** ([Exrpg](http://bbs.exrpg.com/space-uid-137285.html)) MapRender的原始代码 以及libgif
- **[DotNetBar](http://www.devcomponents.com/)**
- **[IMEHelper](https://github.com/JLChnToZ/IMEHelper)**
- **[Spine-Runtime](https://github.com/EsotericSoftware/spine-runtimes)**
- **[EmptyKeysUI](https://github.com/EmptyKeys)**
- **[@KENNYSOFT](https://github.com/KENNYSOFT)** and his WcR2-KMS version.
- **[@Kagamia](https://github.com/Kagamia)** and her WcR2-CMS version.
- **[@Spadow](https://github.com/Sunaries)** for providing his WcR2-GMS version.
- **[@PirateIzzy](https://github.comPirateIzzy)** for providing the basis of this fork.
- **[@seotbeo](https://github.com/seotbeo)** for providing Skill comparison feature.