#load "shared/NugetDownload.fsx"
open NugetDownload

let nugetFiles = 
    [| 
        "HDF.PInvoke.NETStandard/1.10.200",
        [|
            yield @"lib/netstandard2.0/HDF.PInvoke.dll"
            match os with
            | Linux -> yield! [|"libhdf5_hl.so";"libhdf5_hl.so.101";"libhdf5.so";"libhdf5.so.101"|] |> Array.map (sprintf @"runtimes/linux-x64/native/%s")
            | Windows -> yield! [|"hdf5_hl.dll";"hdf5.dll";"zlib1.dll"|] |> Array.map (sprintf @"runtimes/win-x64/native/%s")
            | OSX -> yield! [|"libhdf5_hl.dylib"; "libhdf5.dylib"|] |> Array.map (sprintf @"runtimes/osx-x64/native/%s")
        |]
        "TensorFlowSharp/1.11.0",
        [|
            yield @"lib/netstandard2.0/TensorFlowSharp.dll"
            match os with
            | Linux -> yield! [|"libtensorflow_framework.so"; "libtensorflow.so"|] |> Array.map (sprintf @"runtimes/linux/native/%s")
            | Windows -> yield @"runtimes/win7-x64/native/libtensorflow.dll"
            | OSX -> yield! [|"libtensorflow_framework.dylib"; "libtensorflow.dylib"|] |> Array.map (sprintf @"runtimes/osx/native/%s")
        |]
    |]

downloadAndExtractNugetFiles nugetFiles

downloadFile("https://github.com/fchollet/deep-learning-models/releases/download/v0.2/resnet50_weights_tf_dim_ordering_tf_kernels.h5",
             "pretrained/resnet50_weights_tf_dim_ordering_tf_kernels.h5")
