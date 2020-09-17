namespace libaafs

open System.Collections
open libaafs.image

type BlockMap =
    { DimensionXY: int
      Map: Generic.IDictionary<byte [] [], char> }

type private Block =
    { WidthAndHeight: int // it's square, so all we need is one
      Data: byte [] [] // usualy NxN (square) dimension block
      Char: char }

module Map =
    // 4x4 pixel to be mapped to text
    let private map4x4 =
        [ { WidthAndHeight = 4
            Data =
                [| [| 0uy; 0uy |]
                   [| 0uy; 0uy |] |]
            Char = ' ' }
          { WidthAndHeight = 4
            Data =
                [| [| 0uy; 0uy |]
                   [| 0uy; 1uy |] |]
            Char = '.' }
          { WidthAndHeight = 4
            Data =
                [| [| 0uy; 0uy |]
                   [| 1uy; 0uy |] |]
            Char = ',' }
          { WidthAndHeight = 4
            Data =
                [| [| 0uy; 0uy |]
                   [| 1uy; 1uy |] |]
            Char = '_' }
          { WidthAndHeight = 4
            Data =
                [| [| 0uy; 1uy |]
                   [| 0uy; 0uy |] |]
            Char = '\'' }
          { WidthAndHeight = 4
            Data =
                [| [| 0uy; 1uy |]
                   [| 0uy; 1uy |] |]
            Char = '|' }
          { WidthAndHeight = 4
            Data =
                [| [| 0uy; 1uy |]
                   [| 1uy; 0uy |] |]
            Char = '/' }
          { WidthAndHeight = 4
            Data =
                [| [| 0uy; 1uy |]
                   [| 1uy; 1uy |] |]
            Char = '6' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 0uy |]
                   [| 0uy; 0uy |] |]
            Char = '`' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 0uy |]
                   [| 0uy; 1uy |] |]
            Char = '\\' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 0uy |]
                   [| 1uy; 0uy |] |]
            Char = '!' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 0uy |]
                   [| 1uy; 1uy |] |]
            Char = 'L' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 1uy |]
                   [| 0uy; 0uy |] |]
            Char = '-' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 1uy |]
                   [| 0uy; 1uy |] |]
            Char = ']' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 1uy |]
                   [| 1uy; 0uy |] |]
            Char = 'T' }
          { WidthAndHeight = 4
            Data =
                [| [| 1uy; 1uy |]
                   [| 1uy; 1uy |] |]
            Char = '#' } ]
        |> Seq.groupBy (fun dat -> dat.WidthAndHeight)
        |> Seq.map (fun group ->
            { DimensionXY = fst group
              Map =
                  snd group
                  |> Seq.map (fun block -> (block.Data, block.Char))
                  |> dict })

    let private blockMaps = map4x4

    let private lookup dimension byteArray =
        blockMaps
        |> Seq.where (fun block -> block.DimensionXY = dimension)
        |> Seq.tryPick (fun block ->
            match block.Map.ContainsKey(byteArray) with
            | true -> Some <| block.Map.Item byteArray
            | false -> None)
        |> fun b ->
            match b with
            | Some v -> v
            | None -> ' '

    /// process in parallel of 4 quadrants
    let private convertToBlocks dimension dataBlock: char[][] =
        ()
    
    let private readImage filename dimension: libaafs.CellImage =
        image.readPng filename
        |> image.toGreyScale
        |> image.toBlock dimension
        
    let convert filename dimension: char[][] =
        readImage filename dimension
        |> convertToBlocks dimension
