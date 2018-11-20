#load "bufferedGraphics.fsx"

open System
open System.ComponentModel
open System.Drawing
open System.Windows.Forms

let be = new BufferingExample()

BufferedGraphicsManager.Current

be.Show()


