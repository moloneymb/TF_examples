module VGG
open TensorFlow
open System

// TODO make sure TFGraph.Variable uses VariableV2, this is not needed at this stage as we sub in a TFOutput instead of a trainable Variable

type TFGraph with
    member this.Conv2DTranspose(value,filter, output_shape:TFShape, strides, ?padding:string, ?data_format:string,?operName:string) = 
        let paddingV     = defaultArg padding "SAME"
        let data_formatV = defaultArg data_format "NHWC"
        use name = this.WithScope("conv2d_transpose") // NOTE: this needs control parameters
        if not (data_formatV = "NCHW" || data_formatV = "NHWC") then 
            failwith "dataformat has to be either NCHW or NHWC."
        let axis = if data_formatV = "NHWC" then 3 else 1
        let value_shape = this.GetShape(value)
        let filter_shape = this.GetShape(filter)
        if value_shape.[axis] <> filter_shape.[3] then
            failwithf "input channels does not match filter's input channels, \n %i != %i" 
                value_shape.[axis]
                filter_shape.[3]
        if output_shape.[3] <> filter_shape.[2] then
            failwithf "input channels does not match filter's input channels, \n %i != %i" 
                value_shape.[axis]
                filter_shape.[3]
        if paddingV <> "VALID" && paddingV <> "SAME" then
            failwithf "padding must be either VALID or SAME: %s" paddingV

        this.Conv2DBackpropInput(
            input_sizes = this.Const(output_shape.AsTensor()),
            filter = filter,
            out_backprop = value,
            strides = strides,
            padding = paddingV,
            data_format = data_formatV//,
            //?operName = operName // The name pass through does not seem to be working for some reason
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

        let conv_init_vars(input:TFOutput, out_channels:int64, filter_size:int64,is_transpose,name:string) =
            let weights_shape = 
                let in_channels = graph.GetShape(input).[3]
                if not is_transpose then
                    [|filter_size; filter_size; in_channels; out_channels|]
                else
                     [|filter_size; filter_size; out_channels; in_channels|]
            let truncatedNormal = graph.TruncatedNormal(graph.Const(TFShape(weights_shape).AsTensor()),TFDataType.Float, seed=System.Nullable(1L))
            weights.[name + "/weights"]
            //graph.Variable(graph.Mul(truncatedNormal,graph.Const(new TFTensor(0.1f))),operName="weights").Read

        let instance_norm(input:TFOutput, train:bool, name:string) =
            use scope = graph.WithScope(name + "/instance_norm")
            let var_shape = TFShape(graph.GetShape(input).[3])
            let mu, sigma_sq = graph.Moments(input, graph.Const(TFShape([|1L;2L|]).AsTensor()), keep_dims=true)
            let shift = weights.[name + "/shift"]
            let scale = weights.[name + "/scale"]
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
            graph.Add(input, tmp |> conv_layer(128L, filter_size, 1L, true, name + "_c2"))

        let conv_transpose_layer(num_filters:int64, filter_size:int64, strides:int64, name:string) (input:TFOutput) =
            let weights_init = conv_init_vars(input, num_filters, filter_size,true,name)
            match graph.GetShape(input) with
            | [|batch_size; rows; cols; in_channels|] ->
                let new_shape = TFShape(batch_size, rows*strides, cols*strides, num_filters)
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
        |> conv_layer(32L,3L,1L,false,"conv_t3")
        |> fun conv_t3 -> graph.Add(graph.Mul(graph.Tanh(conv_t3), graph.Const(new TFTensor(150.f))), graph.Const(new TFTensor(255.f / 2.f)))
        |> fun x -> graph.ClipByValue(x,graph.Const(new TFTensor(0.f)), graph.Const(new TFTensor(255.f)))
(*
module PretrainedVGG =
    let net(graph:TFGraph, weights:Map<string,TFTensor>, input_img:TFOutput) =

        let layers = [|
            "conv1_1"; "relu1_1"; "conv1_2"; "relu1_2"; "pool1";
            "conv2_1"; "relu2_1"; "conv2_2"; "relu2_2"; "pool2";

            "conv3_1"; "relu3_1"; "conv3_2"; "relu3_2"; "conv3_3"; 
            "relu3_3"; "conv3_4"; "relu3_4"; "pool3"

            "conv4_1"; "relu4_1"; "conv4_2"; "relu4_2"; "conv4_3"; 
            "relu4_3"; "conv4_4"; "relu4_4"; "pool4"

            "conv5_1"; "relu5_1"; "conv5_2"; "relu5_2"; "conv5_3"; 
            "relu5_3"; "conv5_4"; "relu5_4"; 
        |]

        let mean_pixel = graph.Const(new TFTensor([|123.68; 116.779; 103.939|]))
        let weights = failwith "TODO"

        let input_img = graph.Sub(input_img,mean_pixel)

        let output = 
            (input_img,layers) ||> Array.foldi (fun input (i,name) ->
                let kind = name.[..4]
                match kind with
                | "conv" -> 
                    let kernels, bias = failwith "todo"//weights.[i].[0].[0].[0].[0]
                    let kernels = failwith "todo" // graph.Transpose(kernels, [1L;0L;2L;3L])
                    let bias = graph.Reshape(bias,graph.Const(TFShape(-1L).AsTensor()))
                    graph.Conv2D(input,kernels,[|1L;1L;1L;1L|],"SAME")
                | "relu" -> graph.Relu(input)
                | "pool" -> graph.MaxPool(input, ksize=[|1L;2L;2L;1L|], strides=[|1L;2L;2L;1L|], padding="SAME")
                | _ -> failwith "layer name prefix not found"
            ) 

        graph.Add(output,mean_pixel)
*)


