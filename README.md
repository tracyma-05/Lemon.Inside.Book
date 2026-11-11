# The Inside Book · 看见原理

An interactive book about how things work, seen from the inside: light and shadow, gears, the mechanical watch.
Every idea is one spread — read on the left, try it on the right: drag a picture to look round it, move a slider and
watch what changes. English and Chinese.

一本讲“东西是怎么工作的”的互动书：光与影、齿轮、机械表。每个道理占两页，左边读，右边动手试——拖动画面转着看，拉动滑块看什么在变。中英双语。

**Status:** P0 — the skeleton and a rendering test (see [`docs/ROADMAP.md`](docs/ROADMAP.md), in Chinese).

## Build and run

Needs the .NET 11 SDK (and the `wasm-tools` workload for the web edition).

```bash
dotnet run --project src/Lemon.Inside.Book.Desktop
```

```bash
dotnet test
```

The web edition is published with WebAssembly AOT in Release:

```bash
dotnet publish src/Lemon.Inside.Book.Browser -c Release -o artifacts/web
```

## How it is made

- [Avalonia 12](https://avaloniaui.net/) on .NET 11: one code base for Windows, macOS and the browser.
- The book itself (pages, page turning, narration, bookmarks, notes, settings, themes, updates) is the
  `Lemon.Book.Shell` package, shared with the other Lemon books.
- The 3D pictures are drawn on the CPU (`src/Lemon.Inside.Book/Controls/Scene`): a small software rasterizer and a
  square lamp whose light and soft shadows are worked out per vertex, so the same code runs on the desktop and in the
  browser.

## Credits

The topics are inspired by Bartosz Ciechanowski's wonderful interactive articles at
[ciechanow.ski](https://ciechanow.ski/). The text, pictures and code in this book are our own.

Code: [MIT licence](LICENSE). Fonts and other third-party material: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
