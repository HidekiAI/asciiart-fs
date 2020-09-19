namespace libaafs

open System.Collections    // IDictionary
open libaafs

type BlockMap =
    { DimensionXY: uint
      Map: Generic.IDictionary<byte [] [], char> }

type private CharBlock =
    { WidthAndHeight: uint // it's square, so all we need is one
      Data: byte [][] // NxN (square) dimension block, where it's [Y][X]
      Char: char }

//€‚ƒ„…†‡ˆ‰Š‹ŒŽ‘’“”•–—˜™š›œžŸ¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ
module Map =
    // 4x4 pixel to be mapped to text
    let private map4x4 =
        [ { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 0uy |]
                   [| 0uy; 0uy |] |]
            Char = ' ' }
          { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 0uy |]
                   [| 0uy; 1uy |] |]
            Char = '¸' }
          { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 0uy |]
                   [| 1uy; 0uy |] |]
            Char = '¡' }
          { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 0uy |]
                   [| 1uy; 1uy |] |]
            Char = '„' }
          { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 1uy |]
                   [| 0uy; 0uy |] |]
            Char = '´' }
          { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 1uy |]
                   [| 0uy; 1uy |] |]
            Char = '|' }
          { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 1uy |]
                   [| 1uy; 0uy |] |]
            Char = '/' }
          { WidthAndHeight = 4u
            Data =
                [| [| 0uy; 1uy |]
                   [| 1uy; 1uy |] |]
            Char = '6' }    // depending on font, it can be `&` as well
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 0uy |]
                   [| 0uy; 0uy |] |]
            Char = '‘' }
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 0uy |]
                   [| 0uy; 1uy |] |]
            Char = '\\' }
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 0uy |]
                   [| 1uy; 0uy |] |]
            Char = '!' }
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 0uy |]
                   [| 1uy; 1uy |] |]
            Char = 'L' }
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 1uy |]
                   [| 0uy; 0uy |] |]
            Char = '¯' }
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 1uy |]
                   [| 0uy; 1uy |] |]
            Char = '¶' }    // '¬'
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 1uy |]
                   [| 1uy; 0uy |] |]
            Char = 'ƒ' }
          { WidthAndHeight = 4u
            Data =
                [| [| 1uy; 1uy |]
                   [| 1uy; 1uy |] |]
            Char = '©' }    // I also like '#', '*', '%' and '@'
          ]
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
            match block.Map.ContainsKey(byteArray) with    // TODO: Replace this ContainsKey with mapping to take advantage of F#
            | true -> Some <| block.Map.Item byteArray
            | false -> None)
        |> fun b ->
            match b with
            | Some v -> v
            | None -> ' '

    let blockToBitMap byteBlock =
        byteBlock
        |> Array.map(fun row ->
            row
            |> Array.map(fun col ->
                if col > 0uy then 1uy
                else 0uy))

    /// process in parallel of 4 quadrants
    let private convertToBlocks (dataBlock: CellImage): char[][] =
        let charMap = Array.zeroCreate (int dataBlock.CellHeight + 1)
        // pack byte array into NxN
        for cellY in 0u..(dataBlock.CellHeight - 1u) do
            let charRow = Array.zeroCreate (int dataBlock.CellWidth + 1)
            for cellX in 0u..(dataBlock.CellWidth - 1u) do
                let cell = dataBlock.Cells.[int cellY].[int cellX]
                charRow.[int cellX] <- lookup cell.Dimension (blockToBitMap cell.Block)
            charMap.[int cellY] <- charRow
        charMap

    let private readImage filename dimension: CellImage =
        image.readPng filename
        |> image.toGreyScale
        |> image.toBlock dimension  // make a block of NxN
        
    let convert filename dimension: char[][] =
        readImage filename dimension
        |> convertToBlocks
