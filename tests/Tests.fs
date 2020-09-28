module Tests

//open System
open System.Drawing
open System.Text
open Xunit
open Xunit.Abstractions

open libaafs

type MyTests(output: ITestOutputHelper) =
    [<Fact>]
    let ``Tests RawRGBImage type`` () =
        // make sure imageHeight is 8 or greater, for unit test is looking for specific index close to end of image
        let imageHeight = 11u // a bug of last scanline should be verified, so make sure this is divisible by 4

        let image10xH: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = 10u // having it width of 10 makes it easier to visually verify data, also will verify truncated block
              Height = imageHeight
              Data =
                  [|
                  // to make test predictable, N-th color will be N
                  for p in 0 .. ((10 * (int imageHeight)) - 1) do
                      { R = byte p &&& 255uy
                        G = byte p &&& 255uy
                        B = byte p &&& 255uy
                        A = 127uy } |] } // 50% translucent

        let sb = StringBuilder()
        image.dumpRGBA sb image10xH
        output.WriteLine(sb.ToString())

        Assert.True(image10xH.Data.[63].R = 63uy)
        if image10xH.Data.[63].R <> 63uy then failwith "Expected last data to be 63"

    [<Fact>]
    //let ``Tests RawRGBImage type`` () =
    let ``Tests ByteImage type`` () =
        // make sure imageHeight is 8 or greater, for unit test is looking for specific index close to end of image
        let imageHeight = 11u // a bug of last scanline should be verified, so make sure this is divisible by 4

        let image10xH: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = 10u // having it width of 10 makes it easier to visually verify data, also will verify truncated block
              Height = imageHeight
              Data =
                  [|
                  // to make test predictable, N-th color will be N
                  for p in 0 .. ((10 * (int imageHeight)) - 1) do
                      { R = byte p &&& 255uy
                        G = byte p &&& 255uy
                        B = byte p &&& 255uy
                        A = 127uy } |] } // 50% translucent

        let greyScale10xH = image.toRawLibPixelImage image10xH
        let sb = StringBuilder()
        image.dumpByteImage sb greyScale10xH
        output.WriteLine(sb.ToString())
        Assert.True(greyScale10xH.Pixels.[63].Compressed = 63uy)
        if greyScale10xH.Pixels.[63].Compressed <> 63uy // 0x4D
        then failwith "Expected last data in greyscale to be 63"
        // bottom right of last block:
        if greyScale10xH.Pixels.[((4 * 10 * 2) - (10 - (2 * 4)) - 1)].Compressed
           <> 77uy then // 0x4D
            failwith "Expected last data in greyscale to be 77 (0x4D)"

    [<Fact>]
    let ``Tests NO BlockImage`` () =
        let dimension = 4 // we'll test for 4x4 cells

        let data =
            [|
            // to make test predictable, N-th color will be N
            for p in 0 .. (10 * 3) do // 10 wide, 3 high (2 pixels extra on edges, but 1 scanline short)
                { R = byte p &&& 255uy
                  G = byte p &&& 255uy
                  B = byte p &&& 255uy
                  A = 127uy } |] // 50% translucent

        let image10xH: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = 10u // having it width of 10 makes it easier to visually verify data, also will verify truncated block
              Height = 3u
              Data = data }

        let greyScale10xH = image.toRawLibPixelImage image10xH

        //Assert.Equal<Collections.Generic.IEnumerable<int>>(expected, actual)
        let block =
            image.toBlock (uint32 dimension) greyScale10xH
        let myDelegate =
            System.Func<_> (fun () -> image.toBlock (uint32 dimension) greyScale10xH)
        //Assert.Throws<System.Exception>(myDelegate.Invoke)
        block

    [<Fact>]
    let ``Tests BlockImage type`` () =
        let dimension = 4 // we'll test for 4x4 cells
        // make sure imageHeight is 8 or greater, for unit test is looking for specific index close to end of image
        let imageHeight = 11u // a bug of last scanline should be verified, so make sure this is divisible by 4

        let image10xH: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = 10u // having it width of 10 makes it easier to visually verify data, also will verify truncated block
              Height = imageHeight
              Data =
                  [|
                  // to make test predictable, N-th color will be N
                  for p in 0 .. ((10 * (int imageHeight)) - 1) do
                      { R = byte p &&& 255uy
                        G = byte p &&& 255uy
                        B = byte p &&& 255uy
                        A = 127uy } |] } // 50% translucent

        let greyScale10xH = image.toRawLibPixelImage image10xH

        let blocks =
            image.toBlock (uint32 dimension) greyScale10xH

        let sb = StringBuilder()
        image.dumpCellImage sb blocks
        output.WriteLine(sb.ToString())
        Assert.True(blocks.Cells.[1].[1].Block.[3].[3].Compressed = 77uy)
        if blocks.Cells.[1].[1].Block.[3].[3].Compressed
           <> 77uy then
            failwith "Expected bottom corner block's (block (1, 1)) bottom corner (3, 3) to be 77 (0x4D hex)"

    [<Fact>]
    let ``Test blocks conversion`` () =
        let dimension = 4u // we'll test for 4x4 cells

        let white =
            { R = 255uy
              G = 255uy
              B = 255uy
              A = 127uy }

        let black = { R = 0uy; G = 0uy; B = 0uy; A = 127uy }

        let whiteOnLeftRow =
            [| white
               white
               white
               white
               black
               black
               black
               black
               white // extra that won't fit in the cell
               black
               white |]

        let whiteOnRightRow =
            [| black
               black
               black
               black
               white
               white
               white
               white
               black // extra that won't fit in the cell
               black
               black |]

        let whiteBlockOnLeft =
            [| whiteOnLeftRow
               whiteOnLeftRow
               whiteOnLeftRow
               whiteOnLeftRow |]
            |> Array.collect (fun row -> row)

        let whiteBlockOnRight =
            [| whiteOnRightRow
               whiteOnRightRow
               whiteOnRightRow
               whiteOnRightRow |]
            |> Array.collect (fun row -> row)

        // make sure that if image is not block divisible test
        let halfBlock =
            [| whiteOnRightRow
               whiteOnRightRow
               whiteOnLeftRow |]
            |> Array.collect (fun row -> row)

        let myBlocks =
            [| whiteBlockOnLeft
               whiteBlockOnRight
               halfBlock |]
            |> Array.collect (fun row -> row)

        let imageWidth = uint32 whiteOnLeftRow.Length
        let imageHeight = uint32 myBlocks.Length / imageWidth

        let image8x8: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = imageWidth
              Height = imageHeight
              Data = myBlocks }

        let greyScale8x8 = image.toRawLibPixelImage image8x8

        let blocks =
            image.toBlock (uint32 dimension) greyScale8x8

        let bitBasedImage = image.getConvertedBlocks blocks
        let converted = CharMap.convertToBlocks bitBasedImage
        let sb = StringBuilder()
        CharMap.dumpCharMap sb ColorType.NONE converted
        let strImage = sb.ToString()
        output.WriteLine(strImage)
        //  --
        // |© |
        // | ©|
        //  --
        Assert.True(strImage.Contains("|© |"))
        Assert.True(strImage.Contains("| ©|"))

    [<Fact>]
    let ``Tests colorization of the image`` () =
        // for code in {0..255}; do echo -e "\e[38;05;${code}m $code: Test"; done
        let dimension = 4u // we'll test for 4x4 cells
        // make sure imageHeight is 8 or greater, for unit test is looking for specific index close to end of image
        let imageDimension = dimension * 2u

        let green =
            { R = 0uy
              G = 255uy
              B = 0uy
              A = 127uy }

        let red =
            { R = 255uy
              G = 0uy
              B = 0uy
              A = 127uy }

        let blue =
            { R = 0uy
              G = 0uy
              B = 255uy
              A = 127uy }

        let cyan =
            { R = 0uy
              G = 200uy
              B = 200uy
              A = 127uy }

        let greenRed =
            [| green
               green
               green
               green
               red
               red
               red
               red |]

        let blueCyan =
            [| blue
               blue
               blue
               blue
               cyan
               cyan
               cyan
               cyan |]

        let greenRedBlocks =
            [| greenRed
               greenRed
               greenRed
               greenRed |]
            |> Array.collect (fun row -> row)

        let blueCyanBlocks =
            [| blueCyan
               blueCyan
               blueCyan
               blueCyan |]
            |> Array.collect (fun row -> row)

        // make sure that if image is not block divisible test
        let halfBlock =
            [| greenRed; blueCyan |]
            |> Array.collect (fun row -> row)

        let myBlocks =
            [| greenRedBlocks
               blueCyanBlocks
               halfBlock |]
            |> Array.collect (fun row -> row)

        let imageWidth = imageDimension
        let imageHeight = uint32 myBlocks.Length / imageWidth

        let image8x8: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = imageWidth
              Height = imageHeight
              Data = myBlocks }

        let greyScale8x8 = image.toRawLibPixelImage image8x8

        let blocks =
            image.toBlock (uint32 dimension) greyScale8x8

        let bitBasedImage = image.getConvertedBlocks blocks
        let converted = CharMap.convertToBlocks bitBasedImage
        let sb = StringBuilder()
        CharMap.dumpCharMap sb ColorType.ASCII converted
        let strImage = sb.ToString()
        CharMap.dumpCharMap sb ColorType.HTML converted
        let strImage = sb.ToString()
        output.WriteLine(strImage)
