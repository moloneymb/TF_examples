open TensorFlow
//TODO enable arbitrary image size by improving on Conv2DTranspose

#I "bin/Debug/netstandard2.0"
#r "netstandard"
#r "TensorFlowSharp.dll"
#load "NPYReaderWriter.fsx"
#load "BitmapWriter.fsx"

open NPYReaderWriter
open System
open System.IO
open TensorFlow

if not System.Environment.Is64BitProcess then System.Environment.Exit(-1)

fsi.AddPrinter(fun (x:TFGraph) -> sprintf "TFGraph %i" (int64 x.Handle))

let pretrained_dir = Path.Combine(__SOURCE_DIRECTORY__,"..","..","pretrained")
let weights_path = Path.Combine(pretrained_dir, "fast_style_weights_rain.npz")
let example_dir = Path.Combine(__SOURCE_DIRECTORY__, "..","..","examples")

type TFGraph with
    member this.Conv2DTranspose(value,filter, output_shape:int64[], strides:int64[], ?padding:string, ?data_format:string,?operName:string) = 
        let paddingV     = defaultArg padding "SAME"
        let data_formatV = defaultArg data_format "NHWC"
        use name = this.WithScope("conv2d_transpose") // NOTE: this needs control parameters
        if not (data_formatV = "NCHW" || data_formatV = "NHWC") then 
            failwith "dataformat has to be either NCHW or NHWC."
        let axis = if data_formatV = "NHWC" then 3 else 1
        let value_shape = this.GetShape(value)
        let filter_shape = this.GetShape(filter)
        if output_shape.Length <> 4 then
            failwithf "output_shape must have shape (4,) got %A" output_shape
        if value_shape.[axis] <> filter_shape.[3] then
            failwithf "input channels does not match filter's input channels, \n %i != %i" 
                value_shape.[axis]
                filter_shape.[3]
        if output_shape.[3] <> filter_shape.[2] then
            failwithf "output_shape does does not match filter's output channels, \n %i != %i" 
                value_shape.[axis]
                filter_shape.[3]
        if paddingV <> "VALID" && paddingV <> "SAME" then
            failwithf "padding must be either VALID or SAME: %s" paddingV

        this.Conv2DBackpropInput(
            input_sizes = this.Const(TFShape(output_shape).AsTensor()),
            filter = filter,
            out_backprop = value,
            strides = strides,
            padding = paddingV,
            data_format = data_formatV//,
            //?operName = operName // The name pass through does not seem to be working here for some reason
        )

    /// https://github.com/tensorflow/tensorflow/blob/r1.12/tensorflow/python/ops/nn_impl.py
    member this.Moments(x:TFOutput, ?axes:TFOutput, ?shift, ?name, ?keep_dims) =
        let keep_dimsV = defaultArg keep_dims false
        use name = this.WithScope("moments") // NOTE: this needs control parameters

        let y = if x.OutputType = TFDataType.Half then this.Cast(x,TFDataType.Float) else x
        let mean = this.ReduceMean(y, axes |> Option.toNullable, keep_dims=Nullable(true), operName ="mean")
        let variance = this.ReduceMean(
                         this.SquaredDifference(y, this.StopGradient(mean)), 
                         axes |> Option.toNullable,
                         keep_dims=Nullable(true),
                         operName="variance")

        let maybeSqueezeAndCast (y:TFOutput) = 
            let y = if keep_dimsV then y else this.Squeeze(y)
            if x.OutputType = TFDataType.Half then this.Cast(y,TFDataType.Half) else y
        (mean |> maybeSqueezeAndCast, variance |> maybeSqueezeAndCast)

module Array =
    let enumerate (xs:'a[]) = xs |> Array.mapi (fun i x -> (i,x))
    let foldi (f:'b -> (int*'a) -> 'b) (state:'b) (xs:'a[]) : 'b =
        Array.fold f state (xs |> enumerate) 

module PretrainedFFStyleVGG =

    let net(graph:TFGraph, weights:Map<string,TFOutput>, input_img:TFOutput) =
        // TODO: Create the following using Variables and use a checkpoint loader to load the values
        //       This will require a checkpoint saver/loader to be built

        let conv_init_vars(input:TFOutput, out_channels:int64, filter_size:int64,is_transpose:bool,name:string) =
            //let weights_shape = 
            //    let in_channels = graph.GetShape(input).[3]
            //    if not is_transpose then
            //        [|filter_size; filter_size; in_channels; out_channels|]
            //    else
            //        [|filter_size; filter_size; out_channels; in_channels|]
            //let truncatedNormal = graph.TruncatedNormal(graph.Const(TFShape(weights_shape).AsTensor()),TFDataType.Float, seed=System.Nullable(1L))
            weights.[name + "/weights"]
            //graph.Variable(graph.Mul(truncatedNormal,graph.Const(new TFTensor(0.1f))),operName="weights").Read

        let instance_norm(input:TFOutput, train:bool, name:string) =
            use scope = graph.WithScope(name + "/instance_norm")
            let mu, sigma_sq = graph.Moments(input, graph.Const(TFShape([|1L;2L|]).AsTensor()), keep_dims=true)
            let shift = weights.[name + "/shift"]
            let scale = weights.[name + "/scale"]
            //let var_shape = TFShape(graph.GetShape(input).[3])
            //let shift = graph.Variable(graph.Zeros(var_shape),operName="shift").Read
            //let scale = graph.Variable(graph.Ones(var_shape),operName="scale").Read
            let epsilon = graph.Const(new TFTensor(0.001f))
            // Note: The following would benefit from operator overloads
            let normalized = graph.Div(graph.Sub(input,mu),graph.Pow(graph.Add(sigma_sq,epsilon),graph.Const(new TFTensor(0.5f))))
            graph.Add(graph.Mul(scale,normalized),shift)

        let conv_layer(num_filters:int64, filter_size:int64, strides:int64, is_relu:bool, name:string) (input:TFOutput) = 
            let weights_init = conv_init_vars(input, num_filters, filter_size,false,name)
            let x = instance_norm(graph.Conv2D(input, weights_init, [|1L;strides;strides;1L|], padding="SAME"),true, name)
            if is_relu then graph.Relu(x) else x

        let residual_block(filter_size:int64, name:string) (input:TFOutput) = 
            let tmp = input |> conv_layer(128L, filter_size, 1L, true, name + "_c1")
            graph.Add(input, tmp |> conv_layer(128L, filter_size, 1L, false, name + "_c2"))

        let conv_transpose_layer(num_filters:int64, filter_size:int64, strides:int64, name:string) (input:TFOutput) =
            let weights_init = conv_init_vars(input, num_filters, filter_size,true,name)
            match graph.GetShape(input) with
            | [|batch_size; rows; cols; in_channels|] ->
                let new_shape = [|batch_size; (if rows = -1L then -1L else rows*strides); (if rows = -1L then -1L else cols*strides); num_filters|]
                graph.Relu(instance_norm(graph.Conv2DTranspose(input, weights_init, new_shape, [|1L;strides;strides;1L|], padding="SAME"), true, name))
            | _ -> failwith "shape size is incorrect"

        input_img
        |> conv_layer(32L,9L,1L,true,"conv1")
        |> conv_layer(64L,3L,2L,true,"conv2")
        |> conv_layer(128L,3L,2L,true,"conv3")
        |> residual_block(3L,"resid1")
        |> residual_block(3L,"resid2")
        |> residual_block(3L,"resid3")
        |> residual_block(3L,"resid4")
        |> residual_block(3L,"resid5")
        |> conv_transpose_layer(64L,3L,2L,"conv_t1")
        |> conv_transpose_layer(32L,3L,2L,"conv_t2")
        |> conv_layer(3L,9L,1L,false,"conv_t3")
        |> fun conv_t3 -> graph.Add(graph.Mul(graph.Tanh(conv_t3), graph.Const(new TFTensor(150.f))), graph.Const(new TFTensor(255.f / 2.f)))
        |> fun x -> graph.ClipByValue(x,graph.Const(new TFTensor(0.f)), graph.Const(new TFTensor(255.f)))


let sess = new TFSession()
let graph = sess.Graph

let weights_map = 
            readFromNPZ((File.ReadAllBytes(weights_path)))
            |> Map.toArray 
            |> Array.map (fun (k,(metadata, arr)) -> 
                k.Substring(0, k.Length-4), graph.Reshape(graph.Const(new TFTensor(arr)), graph.Const(TFShape(metadata.shape |> Array.map int64).AsTensor()))) 
            |> Map.ofArray

// TODO figure out how to enable Conv2DTranspose to work on arbitrary shapped inputs
//let input = graph.Placeholder(TFDataType.Float, TFShape(-1L,-1L,-1L,3L),"input")
let input = graph.Placeholder(TFDataType.Float, TFShape(1L,474L,712L,3L),"input")

let output = PretrainedFFStyleVGG.net(graph,weights_map,input)
let input_string = graph.Placeholder(TFDataType.String)
let mean_pixel = graph.Const(new TFTensor([|123.68f; 116.778f; 103.939f|]))

let img = 
    let decoded = graph.Cast(graph.DecodeJpeg(contents=input_string, channels=Nullable(3L)), TFDataType.Float)
    let preprocessed = graph.Sub(decoded,mean_pixel)
    let expanded = graph.ExpandDims(input=preprocessed, dim = graph.Const(new TFTensor(0)))
    //let resized = graph.ResizeBicubic(expanded,graph.Const(new TFTensor([|256;256|])),align_corners=Nullable(true))
    expanded


let img_tf = TFTensor.CreateString(File.ReadAllBytes(Path.Combine(example_dir,"chicago.jpg"))) 

let img_tensor = sess.Run([|input_string|],[|img_tf|],[|img|]).[0]
let rain = sess.Run([|input|],[|img_tensor|],[|output|]).[0]

// NOTE: Assumed NHWC dataformat
let tensorToBmp(batchIndex:int) (imgs:TFTensor) =
    if imgs.TensorType <> TFDataType.Float then failwith "type unsupported"
    match imgs.Shape |> Array.map int with
    | [|N;H;W;C|] ->
        let pixels = 
            [|
                let res_arr = imgs.GetValue() :?> Array
                for h in 0..H-1 do
                    for w in 0..W-1 do
                        let getV(c) = byte <| Math.Min(255.f, Math.Max(0.f, (res_arr.GetValue(int64 batchIndex, int64 ((H-1)-h), int64 w, int64 c) :?> float32)))
                        yield BitConverter.ToInt32([|getV(2); getV(1); getV(0); 255uy|], 0) // NOTE: Channels are commonly in RGB format
            |]
        BitmapWriter.BGRAToBitmap(H,W,pixels)
    | _ -> failwithf "shape %A is unsupported" imgs.Shape


File.WriteAllBytes(Path.Combine(__SOURCE_DIRECTORY__, "chicago_in_rain_style.bmp"), tensorToBmp 0 rain)