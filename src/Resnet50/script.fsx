#r "netstandard"
#I "bin/Debug/netstandard2.0"
#r "HDF.PInvoke.dll"
#r "TensorFlowSharp.dll"
#r "Resnet50.dll"
#nowarn "760"
open TensorFlow
open System
open System.IO


let baseDir = Path.Combine(__SOURCE_DIRECTORY__, "bin", "Debug", "netstandard2.0")
let weights_path = Path.Combine(baseDir, "resnet50_weights_tf_dim_ordering_tf_kernels.h5")
let labels_path = Path.Combine(baseDir,"imagenet1000.txt")

let example_dir = Path.Combine(__SOURCE_DIRECTORY__, "..","..","examples")

let label_map = File.ReadAllLines(labels_path)

let sess = new TFSession()

// NOTE: Graph.ToString() returns the whole protobuf as txt to console
fsi.AddPrinter(fun (x:TFGraph) -> sprintf "TFGraph %i" (int64 x.Handle))

let graph = sess.Graph

let (input,output) = Resnet50.Model.buildResnet(graph,weights_path)

/// This is from TensorflowSharp (Examples/ExampleCommon/ImageUtil.cs)
/// It's intended for inception but used here for resnet as an example
/// of this type of functionalityt 
let construtGraphToNormalizeImage(destinationDataType:TFDataType) =
    let W = 224
    let H = 224
    let Mean = 117.0f
    let Scale = 1.0f
    let input = graph.Placeholder(TFDataType.String)
    let loaded_img = graph.Cast(graph.DecodeJpeg(contents=input,channels=Nullable(3L)),TFDataType.Float)
    let expanded_img = graph.ExpandDims(input=loaded_img, dim = graph.Const(TFTensor(0)))
    let resized_img = graph.ResizeBilinear(expanded_img,graph.Const(TFTensor([|W;H|])))
    let final_img = graph.Div(graph.Sub(resized_img, graph.Const(TFTensor([|Mean|]))), graph.Const(TFTensor([|Scale|])))
    (input,graph.Cast(final_img,destinationDataType))

let img_input,img_output = construtGraphToNormalizeImage(TFDataType.Float)


let classifyFile(path:string) =
    let createTensorFromImageFile(file:string,destinationDataType:TFDataType) =
        let tensor = TFTensor.CreateString(File.ReadAllBytes(file))
        sess.Run(runOptions = null, inputs = [|img_input|], inputValues = [|tensor|], outputs = [|img_output|]).[0]
    let example = createTensorFromImageFile(path, TFDataType.Float)
    let index = graph.ArgMax(output,graph.Const(TFTensor(1)))
    let res = sess.Run(runOptions = null, inputs = [|input|], inputValues = [|example|], outputs = [|index|])
    label_map.[res.[0].GetValue() :?> int64[] |> Array.item 0 |> int]

classifyFile(Path.Combine(example_dir,"example_0.jpeg"))
