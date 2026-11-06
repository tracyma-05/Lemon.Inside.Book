# 新书计划：以“可动手的动画”讲清万物原理（灵感来自 ciechanow.ski）

## Context

- 起因：ciechanow.ski（Bartosz Ciechanowski）的 22 篇长文（光与影、齿轮、机械表、相机与镜头、发动机、自行车、GPS、声音、机翼、月球……）用大量可拖动、可调参数的 3D 动画把原理讲透。希望做一本**通俗易懂**、同样“边玩边懂”的书。
- 已定（来自提问）：
  1. **新仓库**，基于 `Lemon.Book.Shell` 包（不并入 Explore Book；Explore 已有 Lenses/Sound/Flight/Seasons/Tides/MoonPhases，主题会重叠，且形式不同）。
  2. **原创重写**：原站全站 “Copyright © Bartosz Ciechanowski”，无开放许可。只借鉴“选题 + 用交互动画讲原理”的方法；文字、讲解顺序、例子、动画全部自己设计与实现，**不抓取、不翻译、不移植其文本与 JS 代码**。书中“致谢/延伸阅读”页链接原文。
  3. 首批章节：**光与影**、**齿轮 + 机械表**。
- 当前状态：P0 已完成；“光与影”9 个跨页全部写完（P1 + P2 的图与文字，见第 3 节末“P1 进展”“P2 进展”）；讲解与旁白未做。

## 1. 产品形态

- 暂定名 `Lemon.Inside.Book`（中文名待定，如「柠檬 · 看见原理」）；避开 “How It Works” 等现有品牌名。
- 读者：12 岁以上 / 大众。正文不出现公式；需要时放进可折叠的「深入一点」卡片。
- 版式：与 Explore 不同，**一章 = 多个跨页（每个跨页一个概念）**。左页：短文 + 控件（滑块、选项 chip）+ 小插图；右页：大交互图 + 讲解面板（LessonPlayer，逐步高亮、“试一试”条件）。每章从左页开始（沿用 `BookContentsTests` 的规则）。
- 双语 `en` / `zh-CN`，所有字符串进两份 resx；旁白沿用 `Narr_*` + `generate-narration.py` 流程（语音许可问题与 Explore 相同，记为开放项）。

## 2. 新仓库起步（P0）

照 Lemon.English.Book 的 P0 提交（`1ded6b5`：“the English Book on the shared shell”）的方式搭骨架，结构与 Explore 一致：

- `src/Lemon.Inside.Book`（共享）、`.Browser`（wwwroot）、`.Desktop`、`tests/Lemon.Inside.Book.Tests`（xunit.v3 + Avalonia.Headless）。
- 从 Explore 复制并改名：`Directory.Build.props`、`Directory.Packages.props`（Lemon.Book.Shell 0.1.6+，Avalonia 12.1.x）、`NuGet.config`、`.gitattributes`、`.githooks` + `scripts/check-sensitive.sh` + gitleaks、CI（按记忆：CI 手动/仅 tag 发布，测试在本地跑）、`deploy/cloudflare`（AOT 运行时放 R2）。
- `App.ConfigureShell`：新程序集、页数、语言 `en` → `zh-CN`；设置目录 / `localStorage` 前缀用新书自己的名字（一旦发布永不更改）。
- lemon-book-app skill 的三项共享约定直接照搬：启动页下载进度（`download-progress.js`、`download-sizes.json` 任务）、Release 保持 WebAssembly AOT、护眼主题（已在 Shell 包中的部分不再复制）。
- 从 Explore 复用（复制进新仓库，或若通用则上移到 Shell 包）：`Controls/Space/Space3D.cs`（Point3D/Vector3D）、`PaintContext`、`SpaceDrawing`（WritePixels/DrawSmooth）、`LabelCache`、`ExploreTargets`（点击命中）、`Explore/Lessons` 的 `ObjectCard`、`OscillationStat/Choice`、`ChapterTestKit`、`ChapterVideoExport`（`BookClock.BeginStepping/Advance` 逐帧导出）。
  - 判断：多本书都要的放 Shell（先 `pack.sh` 本地验证再发布）；只此书用的留本仓库。

## 3. 关键技术：交互 3D 怎么画（最大风险）

现状：Explore 全部用 CPU 绘制（`DrawingContext` + `WriteableBitmap`，`Parallel.For` 填行；`BlackHole/ShadowRenderer.cs` 是逐像素光线追踪先例），无 SkiaSharp 着色器、无 GPU。注意 **WASM 默认单线程**，`Parallel.For` 在网页版不提速。

推荐方案：**新建一个小型场景层 `Controls/Scene`**，以 CPU 为基线：

- `OrbitCamera`：拖动环绕、滚轮/双指缩放；拖动区域需与翻页手势隔离（图内拖动不触发翻页，需确认 Shell `BookView` 的手势让路机制，必要时在 Shell 加开关）。
- `Mesh` + 软件光栅器（z-buffer、逐顶点/逐像素着色）渲染到低分辨率 `WriteableBitmap`，再 `DrawSmooth` 放大；矢量叠加（光线、标注、箭头）仍用 `DrawingContext` 画在上层，保持清晰。
- 光照尽量**解析/预计算**，而非实时路径追踪：
  - 矩形面光源对平面的辐照度有闭式解（多边形光源的形状因子）→ 软阴影 = 被遮挡部分的多边形裁剪。
  - 多次反弹（红墙映红地面）用粗网格**辐射度（radiosity）**迭代，每一次反弹就是一次迭代，正好对应“反弹次数”滑块。
  - 拖动时降分辨率，松手后渐进细化（progressive）。
- **P0 技术验证（spike）**，两条路线对比后定稿：
  - A（基线）：上面的 CPU 方案，在 Release AOT 网页版、普通笔记本上目标 ≥30 fps（拖动时）。
  - B（可选加速）：Avalonia Skia 租约（`ISkiaSharpApiLeaseFeature`）+ `SKRuntimeEffect`（SkSL 着色器），桌面与浏览器（Skia/WebGL）是否都可用、AOT 与无头测试是否可用。只有 A 达不到目标才引入，并保留 A 作为测试/导出回退。
- 2D 机械（齿轮、擒纵）用 `StreamGeometry` 矢量绘制即可，不需要 3D 光栅；机械表用“分层 2.5D / 爆炸图”代替原文的全 3D。

### P0 验证结果（2026-11-06，Windows 笔记本，16 核）

场景：6 m × 6 m 地面（64×64 网格）+ 0.8 m 方盒（每面 8×8）+ 方形面光源，共 4630 个顶点、约 8.6k 个三角形；逐顶点算光照（含遮挡），软件光栅化，`DrawSmooth` 放大。

| 项目 | 桌面 Release（JIT，单线程） | 网页 Release（WASM AOT，单线程） |
|---|---|---|
| 光照 4×4 取样 | 1.7 ms | — |
| 光照 8×8 取样 | 7.0 ms | 32 ms |
| 光照 15–16 取样 | 28.9 ms（16²） | 107 ms（15²） |
| 光栅化约 300×345 | 5.5 ms（320×240） | 3.4 ms |
| 光栅化约 600×690 | 6.3 ms（640×480） | 6.2–7.7 ms |
| 光栅化 1280×960 | 9.2 ms | — |

结论：
- **定稿路线 A（CPU）**，不需要 SkSL 着色器。转动视角只需重新光栅化，网页版满分辨率也在 8 ms 内（60 fps 预算 16 ms）。
- 光栅化的耗时主要在三角形数量，不在像素数：分辨率加倍只多 1–2 ms。所以画面可以用满分辨率，三角形要省着用。
- **瓶颈是光照**：只在灯的参数变化时才重算；网页版 8×8 取样 32 ms，拖滑块会掉到约 20 fps。P1 要做：拖动时用 4×4 取样或更粗的网格，松手后再细化；或者把面光源的无遮挡部分换成闭式解（多边形形状因子），只对影子区域取样。
- 浏览器面板被隐藏时 `requestAnimationFrame` 被节流，测到的 fps（约 30）不可信；上表只记 CPU 耗时。P1 在可见窗口里再测一次 fps。
- 画面：软影子（半影）效果正确；边缘有锯齿（没有抗锯齿），盒子侧面因为只有直射光而偏黑——P2 加反弹光（辐射度）后会改善。

### P0 已完成

- 新仓库 `Lemon.Inside.Book`：从 Explore 的骨架复制，删掉 36 个旧章节、地点功能和声音合成；两卷（光、机械），三章（光与影、齿轮、机械表），后两章是占位页。字符串从 10808 条精简到 107 条，中英两份。
- `Controls/Scene`：`SceneMesh`（细分网格）、`Box`（遮挡）、`AreaLight`（方形面光源 + 软阴影）、`SoftwareRasterizer`（深度缓冲 + 透视校正插值 + sRGB）、`OrbitCamera`（拖动环绕 / 滚轮缩放）。
- “光与影”的验证跨页：灯的高度、大小、位置三个滑块，以及取样数、分辨率、自动旋转三个测试控件，画面左上角显示耗时。
- 测试 57 个全部通过（含新增 `LightShadowTests`）；`SceneBenchmark` 设 `BOOK_BENCH=1` 才运行。
- 还没做：`git` 首次提交；书的图标与封面图（还是 Explore 的太阳系）；GitHub 仓库与发布配置；页面一章多跨页的内容模型（P1 写第二个跨页时再加）。

### P1 进展

- **一章多跨页**：`Chapter(slug, volume, Spreads)`；`BookContents.SpreadPage(章, 节)`；`ChapterRegistry.CreatePages` 返回每个跨页的（文字页, 图页）。每章仍从左页开始（测试覆盖每个跨页）。
- **`SceneView`**（`Controls/Scene`）：3D 图的基类——环绕相机、拖动/滚轮、叠加的平面绘制（`Project` 把 3D 点投到屏幕）、**先粗后细的光照**：拖滑块时用少量取样（影子页 4×4），停下 0.25 秒后算完整的（10×10）。画面改为满分辨率（耗时主要在三角形数，不在像素数）。
- **“光与影”现有 5 个跨页**（文字原创，中英双语，每页有“深入一点”）：
  1. 一盏灯有多亮？——3D 小灯照地板 + “看起来多亮”曲线（sRGB 曲线、过曝变白），滑块：功率。
  2. 斜着照，就暗了——侧视图：手电筒照倾斜木板，光斑变长、变暗；正面光斑小图。滑块：角度。
  3. 离得越远，越暗——3D 线框：穿过 1 米处一个方格的光，在 d 米处铺满 d² 格。可拖动转看。滑块：距离。
  4. 看起来有多大？——俯视：眼睛、汽车、两条视线的夹角，弧长 ÷ 半径 = 弧度；“眼睛看到的”小图。滑块：距离。
  5. 影子为什么有软有硬——P0 的方灯和盒子（去掉了测试控件）。滑块：高度、大小、位置。
- 测试 61 个通过：各跨页能画出、模型数值（cos、平方反比、角度）、两种语言都有每页的字、滑块先粗后细。
- 还没做：讲解（LessonStep + 旁白）、可点的物体卡片；网页版在可见窗口里的帧率；第 6–9 节（半球、辐亮度、反射、颜色与反弹）留到 P2。

### P2 进展：第 5–9 节

“光与影”现在 9 个跨页，按阅读顺序：亮度 → 斜照 → 距离 → 角度 → **半球天空** → **辐亮度** → **反射** → 软硬影子 → **颜色与反弹**。

- 5 地板上的一点“看见”了什么（`SkyScene`，3D 线框可转动）：点上方的半球、灯投到半球上的一块、再压扁到地面圆上的一块；占圆的比例 = 这一点分到的光（视因子），把第 2、3 节合成一张图。滑块：灯的高度角、距离。
- 6 为什么墙远看近看一样亮（`RadianceDiagram`，侧视）：眼睛的视锥照到发光板，看到的面积 ∝ d²/cos，每块送来的光 ∝ cos/d²，乘积恒为 1。滑块：距离、板转开的角度。
- 7 镜面、光滑面和毛面（`ReflectionDiagram`，侧视）：入射光、离开时的光形（镜面尖刺 ↔ 毛面圆），眼睛看到的亮度。滑块：入射角、粗糙度、眼睛角度。
- 9 颜色与反弹（`BounceScene`，3D）：白地板、白盒子、彩色墙（红/绿/蓝）；`Controls/Scene/Radiosity` 逐顶点两两传光，每次反弹一次迭代；滑块 0–3 次反弹。拖动时每 4 个顶点取 1 个（粗），停下后全算。
  - 简化：反弹不检查遮挡（盒子底下会漏一点反弹光），对地板 + 墙 + 盒子这样开阔的场景够用。
- 耗时（桌面 Release，约 1135 个顶点）：1/2/3 次反弹 7.5/11.8/18.7 ms，粗算 3 次 5 ms。按 P0 测得网页约慢 4.5 倍估算：全算约 85 ms（只在停下后算一次），粗算约 23 ms。
- 修正：静态画刷与画笔改为不可变（`ImmutableSolidColorBrush` / `ImmutablePen`），否则不同 UI 线程（测试）会报错。
- 抗锯齿：3D 图静止时按 2×2 超采样画（每像素 4 个样本，按线性光平均再转回屏幕值），拖动、滚轮或拉滑块时先画 1×，停下 0.25 秒后变平滑。桌面 640×480 的平滑一帧约 11 ms（画 1280×960 约 9 ms + 缩小 1.8 ms）。
- 测试 65 个通过：新增辐亮度恒定、半球占比随距离和斜度下降、镜面与毛面的光形、反弹把白地板染上墙的颜色。

## 4. 首批章节大纲（原创讲法）

### 第 1 章 光与影（约 9 个跨页）
1. 光有多亮：一盏灯的功率与“看起来多亮”不是线性关系（滑块调功率，亮度曲线）。
2. 斜着照更暗：同一束光照到倾斜的纸上被摊薄（手电筒照桌面的例子；调角度）。
3. 越远越暗：平方反比（光线穿过越来越大的球面；调距离）。
4. 看起来有多大：角度与立体角（远处的车变小；从“角度”过渡到“立体角”，滑块调距离）。
5. 表面“看见”的天空：半球投影，解释第 2、3 页其实是一回事。
6. 为什么墙远近一样亮：辐亮度（眼睛/相机测的是什么）。
7. 镜面、毛面与介于二者之间：反射类型（调光源与视线方向）。
8. 影子为什么有软有硬：面光源大小与距离（调灯的大小、高度）。
9. 颜色与反弹：彩色光相加、彩色表面吸收；红墙把地面染红（反弹次数滑块），阴天的“天空光”与贴地阴影收尾。

### 第 2 章 齿轮（约 6 个跨页）
转动与传动比 → 齿数与转速/力矩 → 齿形为何是渐开线（滚动展开的绳子，动画生成齿廓）→ 啮合与压力角 → 齿轮系与减速 → 行星齿轮（固定不同部件得到不同传动）。

### 第 3 章 机械表（约 6 个跨页）
发条储能 → 轮系把一天分成时分秒 → 擒纵：让能量一格一格放出（擒纵轮 + 擒纵叉慢放）→ 摆轮游丝：计时的“心跳”（调游丝硬度看频率）→ 上链与自动陀 → 全机芯爆炸图（可逐层隐藏）。

每个跨页：1 个主交互图、2–4 个控件、3–6 步讲解（`LessonStep`：`Highlight` 对应图中对象 id，`TryIt` 检查读者是否动手），可点对象弹出 `ObjectCard`。

## 5. 分期

| 期 | 内容 | 完成标准 |
|---|---|---|
| P0 | 新仓库骨架 + 网页/桌面能跑空书 + 3D 技术验证（**已完成，未提交**） | `dotnet test` 通过；Release AOT 网页版启动页进度正常；spike 报告与 fps 数据，定稿渲染路线 |
| P1 | `Controls/Scene` 场景层 + 光与影第 1–4 页（**第一批已完成**：5 个跨页，讲解与旁白未做） | 拖动环绕、调参实时；章节测试（目标有文字、讲解 key 存在、跨页渲染与响应） |
| P2 | 光与影第 5–9 页（辐射度、软阴影）（**图与文字已完成**） | 网页版拖动 ≥30 fps（降分辨率）、松手细化 |
| P3 | 齿轮 | 渐开线生成与啮合动画正确（模型测试校验传动比、啮合不穿插） |
| P4 | 机械表 | 擒纵节拍与摆轮频率由模型驱动，`BookClock` 逐帧导出视频可复现 |
| P5 | 旁白、发布（Windows/macOS/网页） | `NarrationManifestTests` 通过；按现有 release 流程出包 |
| 以后 | 相机与镜头、颜色空间/透明叠加、发动机、自行车、GPS、声音、机翼、月球、四维立方体等 | 每章单独立项；与 Explore 重叠的主题换角度或改为链接 |

## 6. 风险与开放项

- **版权**：坚持原创；评审时对照原文，避免句子、示意图构图、讲解顺序逐页雷同。可选：写信告知/致谢作者。
- **性能**：WASM 单线程 + 软件光栅；靠低分辨率 + 解析光照 + 渐进细化；spike 不达标再上 SkSL。
- **手势冲突**：图内拖动 vs 翻页，可能需要 Shell 改动（改在 Lemon.Book.Shell，再打包）。
- **旁白语音许可**：沿用 Explore 的 edge-tts 流程仍有许可问题，与 Explore 一并解决。
- 书名、中文名、图标待定。

## 7. 关键文件（参考 / 将新建）

- 参考：`Lemon.Explore.Book/docs/ROADMAP.md`（“Porting a chapter” 与移植对照表）、`src/Lemon.Explore.Book/Chapters/Seasons/*`（章节模板）、`Contents/BookContents.cs`、`Contents/ChapterRegistry.cs`、`Controls/Space/Space3D.cs`、`Controls/Space/SpaceDrawing.cs`、`Chapters/BlackHole/ShadowRenderer.cs`（逐像素渲染先例）、`tests/.../ChapterTestKit.cs`、`ChapterVideoExport.cs`；Lemon.English.Book 提交 `1ded6b5`（新书起步清单）。
- 新建（新仓库）：`docs/ROADMAP.md`（本计划的中文定稿）、`src/Lemon.Inside.Book/Controls/Scene/*`、`Chapters/LightShadow/*`、`Chapters/Gears/*`、`Chapters/Watch/*`。

## 8. 验证方式

- 每期：`dotnet test`（含本地化覆盖、目录跨页规则、章节测试、旁白清单）；`BOOK_SNAPSHOT_DIR` 导出帧图人工检查。
- 网页：Release 发布后本地起静态服务器，Chrome DevTools 节流 + 性能面板测拖动帧率；检查启动页进度与控制台无完整性错误。
- 桌面：Windows 本地运行；macOS 待机器验证。

