open System
open libaafs

[<EntryPoint>]
let main argv =
    let dimension = 4
    let block = [|[|0uy; 0uy|]; [|1uy; 1uy|] |]
    let chBlock = libaafs.Map.lookup dimension block
    assert('_' = chBlock)

    0 // return an integer exit code
