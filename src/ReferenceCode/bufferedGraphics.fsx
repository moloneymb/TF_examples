namespace global
//ported from https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bufferedgraphics?view=netframework-4.7.2
open System
open System.ComponentModel
open System.Drawing
open System.Windows.Forms
#r "FSharp.Core"

type BufferingExample() =
    inherit Form()
    let context = BufferedGraphicsManager.Current
    let mutable grafxO = None 
    let mutable bufferingMode = 1
    let bufferingModeStrings = 
        [|
            "Draw to Form withotu OptimizedDoubleBuffering control style"
            "Draw to Form using OptimizedDoubleBuffering control style"
            "Draw to HDC for form"
        |]
    let timer = new System.Windows.Forms.Timer(Interval=100)
    let mutable count = 0uy;
    let black = Brushes.Black

    with
        override this.OnLoad(e:EventArgs) =
            grafxO <- Some(context.Allocate(this.CreateGraphics(), new Rectangle(0,0,this.Width,this.Height)))
            this.Text <- "User double buffering"
            this.SetStyle(ControlStyles.AllPaintingInWmPaint ||| ControlStyles.UserPaint, true)
            context.MaximumBuffer <- Size(this.Width+1, this.Height + 1);
            this.MouseDown.Add(this.MouseDownHandler)
            timer.Tick.Add(this.OnTimer)
            timer.Start()

        member this.DrawToBuffer(g:Graphics) =
            if count > 6uy then
               count <- 0uy
               grafxO |> Option.iter (fun x -> x.Graphics.FillRectangle(Brushes.Black,0,0,this.Width,this.Height))
            else        
                count <- count + 1uy
            // Draw randomly positioned and colored ellipses.
            let rnd = new Random();
            for i in 0 .. 20 do
                let px = rnd.Next(20,this.Width-40)
                let py = rnd.Next(20,this.Height-40)
                g.DrawEllipse(new Pen(Color.FromArgb(rnd.Next(0,255),rnd.Next(0,255), rnd.Next(0,255)),1.f),
                    px, py, px + rnd.Next(0, this.Width-px-20), py+rnd.Next(0,this.Height-py-20));
            g.DrawString("Buffering Mode: " + bufferingModeStrings.[bufferingMode], new Font("Arial",16.f), Brushes.White, 10.f,10.f)
            g.DrawString("Right-click to cycle buffering mode", new Font("Arial", 16.f), Brushes.White, 10.f, 44.f);
            g.DrawString("Left-click to toggle timed display refresh", new Font("Arial", 16.f), Brushes.White, 10.f, 68.f);

        member this.MouseDownHandler(e:MouseEventArgs) =
            if e.Button = MouseButtons.Right then
                if bufferingMode > 3 then
                    bufferingMode <- 0
                    match bufferingMode with
                    | 1 -> base.SetStyle(ControlStyles.OptimizedDoubleBuffer, true)
                    | 2 -> base.SetStyle(ControlStyles.OptimizedDoubleBuffer, false)
                    | _ -> ()
                    // Cause the background to be cleared and redraw
                    count <- 6uy;
                    grafxO |> Option.iter (fun x -> this.DrawToBuffer(x.Graphics))
                else
                    bufferingMode <- bufferingMode + 1            
            else 
                if timer.Enabled then timer.Stop() else timer.Start()
        
        member this.OnTimer(e:EventArgs) =
            grafxO |> Option.iter (fun x -> this.DrawToBuffer(x.Graphics))
            printfn "%A" this.Handle
            if bufferingMode = 2 then grafxO |> Option.iter (fun x -> x.Render(Graphics.FromHdc(this.Handle)))
            else this.Refresh()
            this.Invalidate()

        override this.OnResize(e:EventArgs) =
            context.MaximumBuffer <- new Size(this.Width+1, this.Height+1)
            grafxO |> Option.iter (fun x -> x.Dispose())
            grafxO <- None
            grafxO <- Some(context.Allocate(this.CreateGraphics(), Rectangle(0,0,this.Width,this.Height)))
            count <- 6uy;
            grafxO |> Option.iter (fun x -> this.DrawToBuffer(x.Graphics))
            this.Refresh()
            base.OnResize(e)

        override this.OnPaint(e:PaintEventArgs) = grafxO |> Option.iter (fun x -> x.Render(e.Graphics))