open System
open libaafs

[<EntryPoint>]
let main argv =
    let dimension = 4u
    let filename = @"C:/work/csg/asciiart-fs/test.png"
    let chArray = libaafs.Map.convert filename dimension
    let lines =
        chArray
        |> Array.map(fun row ->
            new String(row) )
    printfn "------------------------------------------------------ %A" filename
    for i in 0..(lines.Length - 1) do
        printfn "|%s|" lines.[i]    // by placing '|' on edges, you can tell if image comes out blank...
    printfn "------------------------------------------------------ %A" filename

    0 // return an integer exit code