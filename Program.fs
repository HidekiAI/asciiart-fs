open System
open System.Text
open libaafs

[<EntryPoint>]
let main argv =
    printfn "<!--"
    let dimension = 4u
    let wd = System.IO.Directory.GetCurrentDirectory()
    let mutable filename = wd + "/" + @"test.png"
    if argv.Length > 1 then
        filename <- argv.[1]
    printfn "Filename: '%A'" filename
    if System.IO.File.Exists(filename) <> true then failwith "Unable to locate requested filename"
    let chArray = libaafs.CharMap.convert filename dimension
    printfn "-->"

    let sb = new StringBuilder()
    CharMap.dumpCharMap sb ColorType.HTML chArray
    printfn "%A" sb

    0 // return an integer exit code
