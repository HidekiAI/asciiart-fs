#r @"bin/Debug/netstandard2.0/libaafs.dll"
// if this was rust, I'd have this test on each module file, but for F#, they don't really believe in unit-tests
open libaafs

// This works at least on both Linux bash and MINGW:
// $for code in {0..255}; do echo -e "\e[38;05;${code}m $code: Test"; done

module testConvert =
    // Reads a 8 x 8 pixels, and verifies that for 4x4 dimension, it returns a 2x2 cell
    let testCellConverter =
        let dimension = 4    // we'll test for 4x4 cells
        let image10x9 =
            {
                // purposefully set dimension to be not divisible by block size of 4x4
                Width = 10u    // having it width of 10 makes it easier to visually verify data
                Height = 9u
                Data = [|
                    // to make test predictable, N-th color will be N
                    for p in 0..((10 * 9) - 1) do
                        {
                            R = byte p &&& 255uy
                            G = byte p &&& 255uy
                            B = byte p &&& 255uy
                            A = 127uy    // 50% translucent
                        }
                |]
            }: RawImageRGB
        image.dumpRGBA image10x9
        if image10x9.Data.[63].R <> 63uy then failwith "Expected last data to be 63"

        let greyScale10x9 =
            image.toGreyScale image10x9
        image.dumpByteImage greyScale10x9
        if greyScale10x9.Pixels.[63] <> 63uy then failwith "Expected last data in greyscale to be 63"

        let blocks =
            image.toBlock (uint32 dimension) greyScale10x9
        if blocks.Cells.[1].[1].Block.[3].[3] <> 77uy then failwith "Expected bottom corner block's bottom corner to be 77 (0x4D hex)"
        image.dumpCellImage blocks
