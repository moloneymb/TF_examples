// TODO build the ability to specify output location beyond original file name in the lib directory.
#r "System.IO.Compression"
#r "System.Runtime.InteropServices.RuntimeInformation"
#r "netstandard"
open System
open System.IO.Compression
open System.IO
open System.Net
open System.Runtime.InteropServices

type OS = | Windows  | Linux | OSX

(*
let os = 
    if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then Linux 
    elif RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then Windows
    elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then OSX
    else failwithf "Unsupported OS %s" RuntimeInformation.OSDescription
*)

let os = 
    let platformId = System.Environment.OSVersion.Platform
    if platformId = PlatformID.MacOSX then OSX
    elif platformId = PlatformID.Unix then Linux
    else Windows

let libPath = Path.Combine(__SOURCE_DIRECTORY__,"..","lib")
let nugetPath = Path.Combine(__SOURCE_DIRECTORY__,"..","nuget")

let downloadFile(url:string,path:string) =
    use wc = new System.Net.WebClient()
    printfn "Downloading %s -> %s" url path
    wc.DownloadFile(url,Path.Combine(__SOURCE_DIRECTORY__,"..",path))
    printfn "Completed %s -> %s" url path

let downloadAndExtractNugetFiles(nugetFiles:(string*string[])[]) =
    Directory.CreateDirectory(libPath) |> ignore
    Directory.CreateDirectory(nugetPath) |> ignore
    for (package,files) in nugetFiles do
        use wc = new WebClient()
        let url = sprintf "https://www.nuget.org/api/v2/package/%s" package
        let nugetFileName = package.Replace("/",".") + ".nupkg"
        let nugetFullFileName = Path.Combine(nugetPath,nugetFileName)
        if not (File.Exists(nugetFullFileName)) then
            printfn "Downloading %s" nugetFileName
            wc.DownloadFile(url,nugetFullFileName)
            printfn "Completed %s" nugetFileName
        use fs = new FileStream(nugetFullFileName,FileMode.Open)
        use zip = new ZipArchive(fs,ZipArchiveMode.Read)
        let zipEntryNameMap = zip.Entries |> Seq.mapi (fun i e -> (e.FullName,i)) |> Map.ofSeq
        for file in files do
            let targetFullFileName = Path.Combine(libPath,Path.GetFileName(file))
            if not (File.Exists(targetFullFileName)) then
                match zipEntryNameMap.TryFind(file) with
                | None -> failwithf "file %s not found in nuget archive %s" file nugetFileName
                | Some(i) -> 
                    let entry = zip.Entries.[i]
                    use entryStream = entry.Open()
                    use entryFileStream = new FileStream(targetFullFileName,FileMode.CreateNew)
                    entryStream.CopyTo(entryFileStream)
