open System
open libaafs

[<EntryPoint>]
let main argv =
    let dimension = 4u
    let filename = @"C:/work/csg/asciiart-fs/test.png"
    let chArray = libaafs.CharMap.convert filename dimension
    printfn "Filename: %A" filename
    CharMap.dumpCharMap chArray

    0 // return an integer exit code