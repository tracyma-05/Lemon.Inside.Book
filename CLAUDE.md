# Lemon.Inside.Book

Cross-platform (Avalonia 12, .NET 10) interactive book on how things work (light, gears, machines), explained with
pictures the reader turns, drags and takes apart: Windows/macOS desktop and a WebAssembly PWA from one shared
project. It started as a copy of Lemon.Explore.Book's skeleton. Its shell (book, page turning, lessons, narration, bookmarks, notes, settings, localization,
themes, updates, the book's clock) is the Lemon.Book.Shell package (a sibling repository, shared with the other Lemon
books). Plan and phases: `docs/ROADMAP.md`.

## Public repository: nothing sensitive, ever

- No credentials, tokens, private host names, internal product names, e-mail addresses or personal data in
  code, comments, strings, docs or commit messages. Use obvious placeholders in examples.
- The book needs no back end and no sign-in. Do not add configuration files with URLs or keys (`appsettings*.json`
  is git-ignored on purpose).
- `scripts/check-sensitive.sh` runs as the pre-commit hook (`git config core.hooksPath .githooks`) and in CI,
  together with gitleaks. Keep it passing; never bypass the hook.

## Original work only

- The topics are inspired by Bartosz Ciechanowski's articles (ciechanow.ski), which are copyrighted with no open
  licence. Never copy, translate or port their text, figures or code; write our own explanations, examples and
  pictures. The credits page names the inspiration.

## The shell is a package

- `Lemon.Book.Shell` (and `.Desktop`, `.Browser`) give the book control, narration, lessons, bookmarks, notes,
  settings stores, localization, themes, updates and the book's clock (`BookClock` / `BookStopwatch`, which the
  chapters' animations and the video export run on). Never copy their code back here: a change every book wants goes
  in Lemon.Book.Shell, then `../Lemon.Book.Shell/scripts/pack.sh <this repository>` (bump its version first) and the
  version in `Directory.Packages.props`; once it works, `../Lemon.Book.Shell/scripts/push-nuget.sh` publishes it.
  `NuGet.config` reads `packages/` (contents git-ignored; its README keeps the folder, which NuGet needs) before
  nuget.org, where the published versions are.
- `App.ConfigureShell` tells the shell about this book (assembly, page count, its languages `en` then `zh-CN`) before
  anything reads the settings; the tests call it too (`TestAppBuilder`). The book has no sign-in, so the shell's sync
  is never started.
- A reader's saved data: the desktop edition's settings in the
  `Lemon.Inside.Book` folder (`JsonFileSettingsStore.DefaultPath("Lemon.Inside.Book")`), the web edition's under
  `inside-book:` in `localStorage` (`LocalStorageSettingsStore("inside-book:")`). Never change a settings key, the
  folder or the prefix: a reader's progress would be lost.
- The look shared by the books is the shell's `Styles/Book.axaml` (its icons too); `App.axaml` adds only the book's
  own styles.

## Localization

- Every user-visible string is a key in both `src/Lemon.Inside.Book/Resources/AppStrings.resx` (English) and
  `AppStrings.zh-CN.resx` (Chinese), added in the same change. Name keys `<Area>_<What>`. The shell's own strings
  (bookmarks, notes, updates, the lesson panel) are in the package in both languages; the book's files keep only the
  ones it words its own way (its name in the update banner and the notes' export).
- XAML: `{loc:Localize Key}`. C#: `LocalizationService.Instance.GetString` / `.Format`. Text a view model builds
  must be raised again on a culture change (`PageViewModel.OnCultureChanged`).
- `LocalizationCoverageTests` fails on keys missing from either file or used in code but undefined.

## Book structure

- The running order is in `Contents/BookContents.cs`. Every chapter must open on a left page so it fills one
  spread; `BookContentsTests` checks this.
- A page is a `PageViewModel` subclass plus a view in `Views/Pages`, mapped in `MainView.axaml`'s data templates.
- The shell's `BookView` shows and turns pages.
- Chapters live in `Chapters/<Topic>/` and are wired in `Contents/ChapterRegistry.cs`; Light and shadow
  (`Chapters/LightShadow`) is the reference. The 3D pictures use the CPU renderer in `Controls/Scene` (mesh, square
  lamp with soft shadows, software rasterizer, orbit camera); it runs on the browser's single thread, so keep its cost
  in mind (`SceneBenchmark`, `BOOK_BENCH=1 dotnet test -c Release --filter SceneBenchmark`).
- After editing a `Narr_*` string, run `python -I scripts/narration/generate-narration.py` so the committed manifest
  (`assets/narration/narration.json`) matches; `NarrationManifestTests` checks it. The mp3s are not in git.
- Every `Narr_*` key is a narration clip and must exist in both languages; walkthrough steps and clickable targets
  are checked per chapter.

## Before committing

```bash
dotnet test
```

Set `BOOK_SNAPSHOT_DIR` to save the headless tests' rendered frames as PNGs for a visual check.
