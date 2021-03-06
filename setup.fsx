#load "shared/NugetDownload.fsx"
open NugetDownload

let nugetFiles = 
    [| 
        "ionic.zlib/1.9.1.5", [|"lib/Ionic.Zlib.dll"|]
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
        // https://eiriktsarpalis.wordpress.com/2013/03/27/a-declarative-argument-parser-for-f/ 
        "Argu/5.1.0",[|"lib/netstandard2.0/Argu.dll"|]
    |]

downloadAndExtractNugetFiles nugetFiles

/// Download Pretrained Weights
[| 
  //yield "empty_checkpoint.zip"; // This is for AttGAN which is not ready yet
  yield! ["rain"; "starry_night"; "wave"] |> Seq.map (sprintf "fast_style_weights_%s.npz")
  yield "imagenet1000.txt"
  yield "resnet_classifier_1000.npz"
|]
|> Seq.iter (fun file -> 
    downloadFile(sprintf "https://s3-us-west-1.amazonaws.com/public.data13/TF_examples/%s" file, 
                 System.IO.Path.Combine(__SOURCE_DIRECTORY__,"pretrained",file)))


printfn "Setup has finished."
