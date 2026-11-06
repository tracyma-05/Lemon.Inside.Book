using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Lemon.Book.Shell.Browser;
using Lemon.Inside.Book;

// The prefix this book saves under. Never change it once published: a reader's progress would be lost.
App.Settings = new LocalStorageSettingsStore("inside-book:");
await JSHost.ImportAsync(WebNarrationAudio.ModuleName, WebNarrationAudio.ModuleUrl);
App.NarrationAudio = new WebNarrationAudio();


await AppBuilder.Configure<App>()
    .ConfigureFonts(fonts => fonts.AddFontCollection(new EmbeddedFontCollection(
        new Uri("fonts:Book", UriKind.Absolute),
        new Uri("avares://Lemon.Inside.Book.Browser/Assets/Fonts", UriKind.Absolute))))
    .With(new FontManagerOptions { DefaultFamilyName = "fonts:Book#Noto Sans SC" })
    .StartBrowserAppAsync("out");
