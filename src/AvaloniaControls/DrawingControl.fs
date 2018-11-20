namespace AvaloniaControls
open System
open System.Runtime.InteropServices
open Avalonia
open Avalonia.Controls
open Avalonia.Media.Imaging
open FSharp.NativeInterop

#nowarn "9"

// See AvaloniaCoreSnow for more details on WriteableBitmap with Avalonia
// https://github.com/ptupitsyn/let-it-snow/blob/master/AvaloniaCoreSnow/SnowViewModel.cs

/// This is a test control which simply emits noise
type DrawingControl() as this =
    inherit ContentControl()
    do  
        let wb = new WriteableBitmap(PixelSize(1000,1000),Vector(90.,90.), Nullable(Platform.PixelFormat.Bgra8888))
        let rand = Random()
        do
            use fb = wb.Lock()
            let ptr =  fb.Address |> NativePtr.ofNativeInt<uint32>
            for i in 0..1000000 do
                NativePtr.set ptr i (uint32 <| rand.Next())
        let img = Image(Height=1000.,Width=1000.,Source=wb)
        let border = Avalonia.Controls.Border(BorderThickness=Thickness(10.),BorderBrush=Media.Brushes.Red,Child=img)
        this.Content <- border