
(*
    WARN: This does not work on linux / mac
    * The error is of System.Runtime: System.TypeLoadException: Could not resolve type with token 01000019 from typeref (expected class 'System.Threading.CancellationToken' in assembly 'System.Runtime...
    * It seems to be related to https://github.com/Azure/azure-functions-vs-build-sdk/issues/160
    * Perhaps this will be resolved with the .net core version of FSI
*)

// nuget references would be nice as per https://github.com/Microsoft/visualfsharp/pull/5850
#r "FSharp.Core"
#r "netstandard.dll"
#I "../../packages/Avalonia/lib/netcoreapp2.0"
#r "Avalonia.Styling.dll"
#r "Avalonia.DesktopRuntime.dll"
#r "Avalonia.Animation.dll"
#r "Avalonia.Base.dll"
#r "Avalonia.Controls.dll"
#r "Avalonia.Input.dll"
#r "Avalonia.Interactivity.dll"
#r "Avalonia.Layout.dll"
#r "Avalonia.Logging.Serilog.dll"
#r "Avalonia.Markup.dll"
#r "Avalonia.Markup.Xaml.dll"
#r "Avalonia.Themes.Default.dll"
#r "Avalonia.Visuals.dll"
#r "Avalonia.OpenGL.dll"
#r "../../packages/System.Runtime/ref/netstandard1.5/System.Runtime.dll"
#r "../../packages/SkiaSharp/lib/netstandard1.3/SkiaSharp.dll"
#r "../../packages/Avalonia.Desktop/lib/netstandard2.0/Avalonia.Desktop.dll"
#r "../../packages/Avalonia.Skia/lib/netstandard2.0/Avalonia.Skia.dll"
#r "../../packages/System.Reactive/lib/netstandard2.0/System.Reactive.dll"
#r "../../packages/System.Threading/lib/netstandard1.3/System.Threading.dll"
#r "../../packages/System.Threading.Tasks/ref/netstandard1.3/System.Threading.Tasks.dll"
#r "bin/Debug/netcoreapp2.1/AvaloniaControls.dll"

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Threading
open AvaloniaControls

App.Start() // This fails in Linux and presumably on Mac

let makeWindow() =
    let win = Window()
    win.Background <- Media.Brushes.Red
    win.Show()

Dispatcher.UIThread.Post(Action(makeWindow))
