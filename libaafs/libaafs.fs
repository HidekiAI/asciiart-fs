namespace libaafs

open System
open System.Collections // IDictionary
open libaafs

// 4x4 pixel to be mapped to text would require 64K combinations (each 4x1 vector has 16 combinations, in which need 4
// vectors to make 4x4 matrix, so 16^4 = 65536) this is ridiculously impossible to represent as lookup map.  A 2x2
// matrix would require 16 combinations (2x1 can have 4 combinations, so 4^2 = 16) which is a bit more reasonable,
// but it's too small to represent an 8x8 ASCII character as 2x2.
// Another approach would be to have a 4x1 and 1x4 vector (possibly 4x2 and 2x4), but that will widen (or taller) the
// image.
// Due to sheer laziness and overwhelmed the the fact that I do NOT want to have a table of 65535 matrices, and not even
// 256 lookup, the initial strategies will will be to split the 4x4 pixels into 2x2 blocks by splitting a 4x4 into 4
// quadrants.  Which will then reduce the lookup down to 16 combinations.  Original thoughts on pattern match was to
// also do XOR map with each map-keys and look for smallest number of bits enabled, and use that as a candidate.
// The approach chosen at the moment is rather, to just fill in the each quadrant if more than 2 or more pixels each
// quadrants are turned on.  But to do this, I would first look at individual R|G|B values, and greyscale it down
// per color into 16 shades.  For example, R-value is 0x10,  G=0x33, and B=0xFF.  In which, shift each bytes right by
// 4 bits will return 0x01|0x03|0x0F, which if averaged will result to 0x06.  Similarly (or quicker by less operation,
// one can just add up `(R+G+B)/3>>4' and will result to 0x06 as well.  On the 16-color grey-scaling, this is about
// 30% bright pixel.
// Initial thought about the approach based on greyscaling the quadrants, was then to determine whether if it wasn't
// bright enough (or closer to black), then it should rather set that quadrant to {0,0,0,0}, or possibly, instead of
// a '#', it would become '.' by tricking the visibilities by size.
//
// Thoughts on lookup strategies:
// Traditionally, for performant lookup, one would combine the 4x4 into a 16-bit (WORD size) hex as a key.



type BlockMap =
    { DimensionXY: uint32
      Map: Generic.IDictionary<byte [] [], char> }

type private CharBlock =
    { WidthAndHeight: uint32 // it's square, so all we need is one
      Data: byte [] [] // NxN (square) dimension block, where it's [Y][X]
      Char: char }

//€‚ƒ„…†‡ˆ‰Š‹ŒŽ‘’“”•–—˜™š›œžŸ¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ
module CharMap =

    let private map4x4 =
        [ { WidthAndHeight = 4u
            // 0|0|0|0
            Data =
                [| [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |] |]
            Char = ' ' }
          { WidthAndHeight = 4u
            Data =
                // 0|0|0|1
                [| [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |] |]
            Char = '¸' }
          { WidthAndHeight = 4u
            // 0|0|1|0
            Data =
                [| [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |] |]
            Char = '¡' }
          { WidthAndHeight = 4u
            // 0|0|1|1
            Data =
                [| [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |] |]
            Char = '„' }
          { WidthAndHeight = 4u
            // 0|1|0|0
            Data =
                [| [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |] |]
            Char = '´' }
          { WidthAndHeight = 4u
            // 0|1|0|1
            Data =
                [| [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |] |]
            Char = '‡' }
          { WidthAndHeight = 4u
            // 0|1|1|0
            Data =
                [| [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |] |]
            Char = '/' }
          { WidthAndHeight = 4u
            // 0|1|1|1
            Data =
                [| [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |] |]
            Char = '6' } // depending on font, it can be `&` as well
          { WidthAndHeight = 4u
            // 1|0|0|0
            Data =
                [| [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |] |]
            Char = '¯' }
          { WidthAndHeight = 4u
            // 1|0|0|1
            Data =
                [| [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |] |]
            Char = '\\' }
          { WidthAndHeight = 4u
            // 1|0|1|0
            Data =
                [| [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |] |]
            Char = 'ƒ' }
          { WidthAndHeight = 4u
            // 1|0|1|1
            Data =
                [| [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |] |]
            Char = 'L' }
          { WidthAndHeight = 4u
            // 1|1|0|0
            Data =
                [| [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 0uy; 0uy |]
                   [| 0uy; 0uy; 0uy; 0uy |] |]
            Char = '¯' }
          { WidthAndHeight = 4u
            // 1|1|0|1
            Data =
                [| [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |]
                   [| 0uy; 0uy; 1uy; 1uy |] |]
            Char = '¶' } // '¬'
          { WidthAndHeight = 4u
            // 1|1|1|0
            Data =
                [| [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 0uy; 0uy |]
                   [| 1uy; 1uy; 0uy; 0uy |] |]
            Char = '€' } // '¬'
          { WidthAndHeight = 4u
            // 1|1|1|1
            Data =
                [| [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |]
                   [| 1uy; 1uy; 1uy; 1uy |] |]
            Char = '©' } ] // I also like '#', '*', '%' and '@'
        |> Seq.groupBy (fun dat -> dat.WidthAndHeight)
        |> Seq.map (fun group ->
            { DimensionXY = fst group
              Map =
                  snd group
                  |> Seq.map (fun block -> (block.Data, block.Char))
                  |> dict })

    let private blockMaps = map4x4

    let private lookup dimension (byteArray: byte [] []) =
#if DEBUG
        //printfn
        //    "Looking up (dimension: %A) %s"
        //    dimension
        //    (byteArray
        //     |> Array.fold (fun str1 row ->
        //         str1
        //         + (row
        //            |> Array.fold (fun str2 col -> str2 + sprintf "%02X " col) "") + "\n") "\n")
#endif
        blockMaps
        |> Seq.where (fun block -> block.DimensionXY = dimension)
        |> Seq.tryPick (fun block ->
            match block.Map.ContainsKey(byteArray) with
            | true ->
                match block.Map.TryGetValue byteArray with
                | true, chVal ->
#if DEBUG
                    //printfn "\tFound: %A" chVal
#endif
                    Some <| chVal
                | false, _ -> None
            | false -> None)
        |> fun b ->
            match b with
            | Some v -> v
            | None -> failwith (sprintf "Unhandled byte-array key: %A" byteArray)

    let blockToBitMap byteBlock =
        byteBlock
        |> Array.map (fun row ->
            row
            |> Array.map (fun col -> if col > 0uy then 1uy else 0uy))

    /// process in parallel of 4 quadrants
    let convertToBlocks (dataBlock: CellImage): char [] [] =
        printfn
            "Converting CellImage: %Ax%A pixels, %Ax%A cells, Dimension=%A cellSize: %A cell blocks"
            dataBlock.Width
            dataBlock.Height
            dataBlock.CellWidth
            dataBlock.CellHeight
            dataBlock.Dimension
            dataBlock.Cells.Length

        let charMap =
            Array.zeroCreate (int (dataBlock.CellHeight)) // init each rows
        // pack byte array into NxN
        for cellY in 0u .. (dataBlock.CellHeight - 1u) do
            let charRow =
                Array.zeroCreate (int dataBlock.CellWidth)

            let toStr (byteArray: byte []) =
                byteArray
                |> Array.fold (fun s b -> sprintf "%s %02X" s b) ""

            for cellX in 0u .. (dataBlock.CellWidth - 1u) do
                let cell = dataBlock.Cells.[int cellY].[int cellX]
                charRow.[int cellX] <- lookup cell.Dimension (blockToBitMap cell.Block)
            charMap.[int cellY] <- charRow
        charMap

    let private readImage filename dimension: CellImage =
        image.readPng filename
        |> image.toGreyScale
        |> image.toBlock dimension // make a block of NxN

    let convert filename dimension: char [] [] =
        readImage filename dimension |> convertToBlocks

    let dumpCharMap (charArray: char [] []) =
        let pl len =
            printf " "
            for i in 0 .. len do
                printf "-"
            printfn " "

        let l = charArray.[0].Length - 1
        pl l

        let lines =
            charArray
            |> Array.map (fun row -> new String(row))

        for i in 0 .. (lines.Length - 1) do
            printfn "|%s|" lines.[i] // by placing '|' on edges, you can tell if image comes out blank...
        pl l
