// TODO: Image input needs pre-processing, output needs post processing
// TODO: Find a way to view images!

// NOTE: System.Drawing.Common is not cross platform
// NOTE: SixLabors.ImageSharp has issues with system dependencies, possibly as I'm using a .netcore version of it. Perhaps a mono version would be better.
// NOTE: For now let's simply create an array save to numpy and check the image in numpy

#I "bin/Debug/netstandard2.0"
#r "netstandard"
#r "TensorFlowSharp.dll"
#load "npyFormat.fsx"
#load "vgg.fs"
//#r "System.Drawing.Common"

open NpyFormat
open VGG
open System
open System.IO
open TensorFlow


let weights_data = (File.ReadAllBytes("/home/moloneymb/EE/Git/TF_examples/pretrained/fast_style_weights_rain.npz"))
let weights_map = readFromNPZ(weights_data)

let sess = new TFSession()
let graph = sess.Graph

let xs = weights_map 
            |> Map.toArray 
            |> Array.map (fun (k,(metadata, arr)) -> 
                k.Substring(0, k.Length-4), graph.Reshape(graph.Const(new TFTensor(arr)), graph.Const(TFShape(metadata.shape |> Array.map int64).AsTensor()))) 
            |> Map.ofArray



let input = graph.Placeholder(TFDataType.Float, TFShape(1L,474L,712L,3L),"input")
let output = PretrainedFFStyleVGG.net(graph,xs,input)
let input_string = graph.Placeholder(TFDataType.String)
let mean_pixel = graph.Const(new TFTensor([|123.68; 116.778; 103.939|]))

let img = 
    let decoded = graph.Cast(graph.DecodeJpeg(contents=input_string, channels=Nullable(3L)), TFDataType.Float)
    let preprocessed = graph.Sub(decoded,mean_pixel)
    // NOTE: Resizing isn't doing
    //let resized = graph.ResizeBicubic(preprocessed,graph.Const(new TFTensor([|256L;256L|])))
    graph.ExpandDims(input=preprocessed, dim = graph.Const(new TFTensor(0)))


let img_tf = TFTensor.CreateString(File.ReadAllBytes("/home/moloneymb/EE/Git/TF_examples/examples/chicago.jpg"))

let img_tensor = sess.Run([|input_string|],[|img_tf|],[|img|]).[0]
let rain = sess.Run([|input|],[|img_tensor|],[|output|]).[0]
    
let value = rain.GetValue()

//rain.Shape


//117.0f
//(img_tensor.GetValue() :?> Array).GetValue(0L,0L,0L,0L)

let xx = rain.GetValue() :?> Array
rain.Shape

NpyFormat.writeArrayToNumpy(xx,rain.Shape |> Array.map int32)

// Flatten and save to numpy
let flatten = 
    [|
        for w in 0..476-1 do
            for h in 0..712-1 do
                for c in 0..3-1 do    
                    yield xx.GetValue(0L, int64 w, int64 h, int64 c) :?> float32
    |]

flatten.Length
xx.GetValue(0L,0L,0L,0L)

let npyOut = NpyFormat.writeArrayToNumpy(flatten,rain.Shape |> Array.map int32)
File.WriteAllBytes("/home/moloneymb/EE/Git/fast-style-transfer/img.npy", npyOut)
    
//flatten.GetLength(0)

//flatten.GetType().GetElementType()
//rain.Shape
//open System.Drawing
//let image = new Bitmap(238,356)

// TODO look at SixLabors.ImageSharp instead of System.Drawing.Common

// This is not the most efficent way to get the values
//xx.GetValue(0L,0L,0L,0L)

//let xx = value :?> Array
//xx.GetValue(1,1,2,3)
//graph.EncodeJpeg()

//#r "netstandard"
//#r "/home/moloneymb/EE/Gt/fast-style-transfer/System.Buffers.dll"
//#r "/home/moloneymb/EE/Git/fast-style-transfer/System.Memory.dll"
//#r "/home/moloneymb/EE/Git/fast-style-transfer/System.Runtime.CompilerServices.Unsafe.dll"
//#r "/home/moloneymb/EE/Git/fast-style-transfer/SixLabors.Core.dll"
//#r "/home/moloneymb/EE/Git/fast-style-transfer/SixLabors.ImageSharp.dll"
//#r "System.Memory"


//open SixLabors.ImageSharp
//open SixLabors.ImageSharp.PixelFormats

//let image = new Image<Rgba32>(238,356)


//let image = new Bitmap(238,356)
