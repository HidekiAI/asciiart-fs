module Tests

//open System
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
    member __.``Tests ByteImage type``() =
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

        let greyScale10xH = image.toGreyScale image10xH
        let sb = StringBuilder()
        image.dumpByteImage sb greyScale10xH
        output.WriteLine(sb.ToString())
        Assert.True(greyScale10xH.Pixels.[63] = 63uy)
        if greyScale10xH.Pixels.[63] <> 63uy
        then failwith "Expected last data in greyscale to be 63"

    [<Fact>]
    member __.``Tests BlockImage type``() =
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

        let greyScale10xH = image.toGreyScale image10xH

        let blocks =
            image.toBlock (uint32 dimension) greyScale10xH

        let sb = StringBuilder()
        image.dumpCellImage sb blocks
        output.WriteLine(sb.ToString())
        Assert.True(blocks.Cells.[1].[1].Block.[3].[3] = 77uy)
        if blocks.Cells.[1].[1].Block.[3].[3] <> 77uy
        then failwith "Expected bottom corner block's (block (1, 1)) bottom corner (3, 3) to be 77 (0x4D hex)"

    [<Fact>]
    member __.``Test blocks conversion``() =
        let dimension = 4u // we'll test for 4x4 cells
        // make sure imageHeight is 8 or greater, for unit test is looking for specific index close to end of image
        let imageDimension = dimension * 2u

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
               black |]

        let whiteOnRightRow =
            [| black
               black
               black
               black
               white
               white
               white
               white |]

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

        let image8x8: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = imageDimension
              Height = imageDimension
              Data =
                  [| whiteBlockOnLeft
                     whiteBlockOnRight |]
                  |> Array.collect (fun row -> row) }

        let greyScale8x8 = image.toGreyScale image8x8

        let blocks =
            image.toBlock (uint32 dimension) greyScale8x8

        let bitBasedImage = image.getConvertedBlocks blocks
        let converted = CharMap.convertToBlocks bitBasedImage
        let sb = StringBuilder()
        CharMap.dumpCharMap sb converted
        let strImage = sb.ToString()
        output.WriteLine(strImage)
        //  --
        // |© |
        // | ©|
        //  --
        Assert.True(strImage.Contains("|© |"))
        Assert.True(strImage.Contains("| ©|"))
