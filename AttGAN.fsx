/// NOTE This is a work in progress
// This is just for the decoder which as been repurposed into an embedder
// This depends on VariableScopes and Layers which are not yet available
#r "netstandard"
#r "lib/TensorFlowSharp.dll"
#load "shared/NNImpl.fsx"
#load "shared/NNOps.fsx"
#load "shared/NPYReaderWriter.fsx"
#load "shared/ImageWriter.fsx"

// This is from https://github.com/LynnHo/AttGAN-Tensorflow/blob/master/models.py
// Which is a tensorflow slim implementation

open NPYReaderWriter
open System
open System.IO
open TensorFlow

type Slim private () =
    let x = 10
    with 
        static member batch_norm(x:TFOutput,?scale:bool, ?updates_collections:'a[],?is_training:bool) = failwith "todo"

type TFGraph with
    /// NOTE: This needs be fleshed out as per 
    /// https://github.com/tensorflow/tensorflow/blob/a6d8ffae097d0132989ae4688d224121ec6d8f35/tensorflow/python/ops/nn_ops.py#L1583
    member this.LeakyRelu(features:TFOutput, ?alpha:float32, ?operName:string) = 
        use scope = this.WithScope("LeakyRelu")
        let alphaV = defaultArg alpha 0.2f
        this.Maximum(this.Mul(this.Const(new TFTensor(alphaV)),features), 
                     features//, 
                     //?operName=operName for some reason this isn't working
                     )

let MAX_DIM = 64 * 16

(*
let encoder(graph:TFGraph, input:TFOutput,n_layers:int, is_training:bool) = 
    let bn x = Slim.batch_norm(x,scale=false, updates_collections=[||], is_training=false)
    let leakyRelu x = graph.LeakyRelu(x)
    let conv2d(dim:int,y:int,stride:int) (x:TFOutput) = failwith "todo"
    let conv_bn_lrelu x = graph.Conv2D(x) |> bn |> leakyRelu
    ()

let decoder() = 
    ()
*)