#r "bin/Debug/netstandard2.0/libaafs.dll"
// if this was rust, I'd have this test on each module file, but for F#, they don't really believe in unit-tests
open libaafs

// This works at least on both Linux bash and MINGW:
// $for code in {0..255}; do echo -e "\e[38;05;${code}m $code: Test"; done

module testConvert =
    // Reads a 8 x 8 pixels, and verifies that for 4x4 dimension, it returns a 2x2 cell
    let testCellConverter =
        let dimension = 4 // we'll test for 4x4 cells
        // make sure imageHeight is 8 or greater, for unit test is looking for specific index close to end of image
        let imageHeight = 11u // a bug of last scanline should be verified, so make sure this is divisible by 4

        let white =
            { R = 255uy
              G = 255uy
              B = 255uy
              A = 127uy }

        let black = { R = 0uy; G = 0uy; B = 0uy; A = 127uy }

        let onLeft =
            [| white
               white
               white
               white
               black
               black
               black
               black
               black
               black |]

        let onRight =
            [| black
               black
               black
               black
               white
               white
               white
               white
               black
               black |]

        let blockOnLeft =
            Array.append
                (onLeft
                 Array.append (onLeft Array.append (onLeft onLeft)))

        let image10xH: RawImageRGB =
            {
              // purposefully set dimension to be not divisible by block size of 4x4
              Width = 10u // having it width of 10 makes it easier to visually verify data, also will verify truncated block
              Height = imageHeight
              Data =
                  Array.append
                      ([|
                       // to make test predictable, N-th color will be N
                       for p in 0 .. ((10 * (int imageHeight)) - 1) do
                           { R = byte p &&& 255uy
                             G = byte p &&& 255uy
                             B = byte p &&& 255uy
                             A = 127uy } |] // 50% translucent
                       // checkered image
                       blockOnLeft) }

        image.dumpRGBA image10xH
        if image10xH.Data.[63].R <> 63uy then failwith "Expected last data to be 63"

        let greyScale10xH = image.toGreyScale image10xH
        image.dumpByteImage greyScale10xH
        if greyScale10xH.Pixels.[63] <> 63uy
        then failwith "Expected last data in greyscale to be 63"

        let blocks =
            image.toBlock (uint32 dimension) greyScale10xH

        image.dumpCellImage blocks
        if blocks.Cells.[1].[1].Block.[3].[3] <> 77uy
        then failwith "Expected bottom corner block's (block (1, 1)) bottom corner (3, 3) to be 77 (0x4D hex)"

        let bitBasedImage = image.getConvertedBlocks blocks

        let converted = CharMap.convertToBlocks bitBasedImage
        CharMap.dumpCharMap converted

// Make sure to close dotnet or else we cannot recompile the DLL
#quit
