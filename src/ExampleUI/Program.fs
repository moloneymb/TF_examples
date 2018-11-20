module Main
// Code Completion seems to break with ProjectReferences for netcoreapp2.0
// This seems to be an issue with FSAC which is surfaced by 
open AvaloniaControls
open Avalonia.Controls
open Avalonia.Threading
open System

[<EntryPoint>]
let main argv =
    //App.Start()
    //let f() =
    //    let win = Window()
    //    win.Show()
    //Dispatcher.UIThread.Post(Action(f))
    printfn "%A" argv
    printfn "running v1"
    Console.ReadLine() |> ignore
    0 
