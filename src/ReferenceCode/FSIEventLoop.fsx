// This is a stub for future work on a FSI Event Loop.
// On initial inspection it does not look like one is needed.
// The creation of controls on the FSI thread throws errors as Avalonia controls need to be 
// created on the UI thread
// It may be possible to modifiy Avolina to enable creation of controls on other 
// threads using the global Dispatcher.UIThread
// This would make it easier to define UIs in F#


//let Create() =
//    let mutable ctSource = new CancellationTokenSource()
//    //let mutable restart = false
//    { new IEventLoop with
//        member this.Run() = 
//            printfn "run Run"
//            // TODO handle if allready running??
//            if ctSource.IsCancellationRequested then ctSource <- new CancellationTokenSource()
//            let app = AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting()
//            app.Instance.Run(ctSource.Token)
//            false
//        member this.Invoke(f:unit->'a) = 
//            printfn "run Invoke"
//            Dispatcher.UIThread.InvokeAsync<'a>(Func<'a>(f),DispatcherPriority.Send) |> Async.AwaitTask |> Async.RunSynchronously
//        member this.ScheduleRestart() = ctSource.Cancel()
//    }
//fsi.EventLoop <- Create()