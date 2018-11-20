namespace AvaloniaControls
open Avalonia.Animation
open System
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml.Styling
open Avalonia.Threading
open System.Threading
open System

type App() =
    inherit Application()
    override this.Initialize() = 
        let baseUri = Uri("resm:base")
        [
            "resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default"
            "resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default"
        ] |> List.iter (fun x -> this.Styles.Add(StyleInclude(baseUri, Source=Uri(x))))
    static member Start() =
        let ctSource = new CancellationTokenSource()
        async {
            try
                let app = AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting()
                let application = app.Instance
                application.Run(ctSource.Token)
            with
            | e -> printfn "%s\n%s" e.Message e.StackTrace;
            } |> Async.Start
