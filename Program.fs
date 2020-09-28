open System
open System.Text
open libaafs

[<EntryPoint>]
let main argv =
    let dimension = 4u
    let wd = System.IO.Directory.GetCurrentDirectory()
    let filename = wd + "/" + @"test.png"
    printfn "Filename: '%A'" filename
    if System.IO.File.Exists(filename) <> true then failwith "Unable to locate requested filename"
    let chArray = libaafs.CharMap.convert filename dimension
    let sb = new StringBuilder()
    CharMap.dumpCharMap sb ColorType.HTML chArray
    printfn "%A" sb

    0 // return an integer exit code
