namespace libaafs

type RGBA = { R: byte; G: byte; B: byte; A: byte }
type RawImageRGB = {
    Width: uint32
    Height: uint32
    // Possibly use System.Drawing.Color here
    Data: RGBA []  // single stream, leave it to width and height to make it 2D
}
type RawImageBytes = {
    Width: uint32
    Height: uint32
    Pixels: byte[]   // only 16 shades of grey are visible to human eyes
}
type Cell = {
    Dimension: uint32    // i.e. 2x2, 4x4, 8x8, etc
    Block: byte[][]    // NOTE: it's [y][x]
}
type CellImage = {
    Width: uint32    // actual raw image dimension
    Height: uint32
    CellWidth: uint32   // i.e. on a 1024 pixels width, at 4x4, CellWidth would be 1024/4 = 256 cells wide
    CellHeight: uint32
    Dimension: uint32    // though this is embedded into Cell record, it's here so that you do not need to pick one of the Cells to lookup its dimensions
    Cells: Cell[][]    // NOTE: it's [y][x]
}

module image =
    open System
    open System.Drawing

    // there are no check to determine if filename has extension '.png', nor does it check (after reading) if it is Bitmap.Png type
    let private read (filename: string): RawImageRGB option =
        try
            let image = new Bitmap(filename) // IDisposable

            // NOTE: This will initialize each cells with NULL, so if you get NULL, it's most likely because of programmer error, in
            // which you did not correctly cover each pixels (i.e. you missed out the edge of the image due to -1, etc)
            let imageArray =
                Array.zeroCreate (image.Width * image.Height) //  pre-allocate array block for performance (way faster than Array.Append! no leak, probably GC kicking in after 1/4 point, gets slower and slower)

            printfn "Opened and read file '%A': Width=%A, Height=%A (Size: %A pixels)" filename image.Width image.Height (image.Width * image.Height)
            { Width = uint32 image.Width
              Height = uint32 image.Height
              Data =
                  for y in 0..(image.Height - 1) do
                      for x in 0..(image.Width - 1) do
                          let pixel: System.Drawing.Color = image.GetPixel(int x, int y)
                          imageArray.[x + ((image.Width - 1) * y)] <-
                                                      { R = pixel.R
                                                        G = pixel.G
                                                        B = pixel.B
                                                        A = pixel.A }
                  imageArray }
            |> Some
        with
        | :? ArgumentOutOfRangeException ->
            printfn "Argument (either X or Y) for Bitmap.GetPixel(x,y) is out of range (programmer error)"
            None
        | :? ArgumentException ->
            printfn "ArgumentExceptions for '%A'" filename
            None
        |  _ ->
                printfn "Unhandled exception"
                None

    let readPng filename: RawImageRGB =
        match read filename with
        | Some image -> image
        | None -> failwith (sprintf "Unable to load %A" filename)
   
    let toGreyScale (rawImage: RawImageRGB): RawImageBytes =
        let arraySize = int (rawImage.Width * rawImage.Height)
        // this method just takes percentages (33%) of each color
        let fromRGBA33 =
            fun (rgba: RGBA) ->
                byte (((int rgba.R) + (int rgba.G) + (int rgba.B)) / 3) &&& 255uy
        // divide by 16 parts, mask it with lower 2 bits, and shift to AARRGGBB bits
        let fromRGBA2Bits =
            fun (rgba: RGBA) ->
                byte (
                         (((rgba.A >>> 4) &&& 3uy ) <<< 6)
                         &&&
                         (((rgba.R >>> 4) &&& 3uy ) <<< 4)
                         &&&
                         (((rgba.G >>> 4) &&& 3uy ) <<< 2)
                         &&&
                         (((rgba.B >>> 4) &&& 3uy ) <<< 0)
                     ) &&& 255uy
        // Similar to 2-bits RGBA, but without the alpha bits which makes it 00RRGGBB
        let fromRGB2Bits =
            fun (rgba: RGBA) ->
                byte (
                         // NOTE: Some techniques bias 2 colors to be 3 bits
                         (((rgba.R >>> 4) &&& 3uy ) <<< 4)
                         &&&
                         (((rgba.G >>> 4) &&& 3uy ) <<< 2)
                         &&&
                         (((rgba.B >>> 4) &&& 3uy ) <<< 0)
                     ) &&& 255uy

        let (pixelArray: byte[]) = Array.zeroCreate (arraySize + 1)
        {
            Width = rawImage.Width
            Height = rawImage.Height
            Pixels =
                for pxy in 0u..((rawImage.Width * rawImage.Height) - 1u) do
                    pixelArray.[int pxy] <- fromRGBA33 rawImage.Data.[int pxy]
                pixelArray
        }

    let private toImageRect width height (byteArray: byte[]): byte[][] =
        let twoDArray = Array.zeroCreate (height + 1)
        for y in 0..(height - 1) do
            let scanLine = Array.zeroCreate (width + 1)
            for x in 0..(width - 1) do
                scanLine.[x] <- byteArray.[x + (y * width)]
            twoDArray.[y] <- scanLine
        twoDArray

    // Cannot assume imageWidth = (cellWidth * dimension) since cellWidth truncates the right edge
    let private makeRowBlocks dimension (cellWidth: uint32) (cy: uint32) (imageWidth: uint32) (arrayedImage: byte[]): Cell[] =
        let cellRow = Array.zeroCreate (int cellWidth + 1)
        for cx in 0u..(cellWidth - 1u) do
            // create a NxN cell block
            let cell = {
                Dimension = dimension; 
                Block = [|
                            for pyRelative in 0u..(dimension - 1u) do
                                let blockRow =
                                    [|
                                        for pxRelative in 0u..(dimension - 1u) do
                                            let px = int ((cx * dimension) + pxRelative)
                                            let py = int ((cy * dimension) + pyRelative)
                                            let pixel = arrayedImage.[px + (py * (int imageWidth))]
                                            pixel
                                    |]
                                blockRow
                        |] 
            }
            cellRow.[int cx] <- cell
        cellRow
   
    let toBlock dimension (rawImageBytes: RawImageBytes): CellImage =
        let cellWidth = rawImageBytes.Width / dimension
        let cellHeight = rawImageBytes.Height / dimension
        {
            Width = rawImageBytes.Width
            Height = rawImageBytes.Height
            CellWidth = uint32 cellWidth
            CellHeight = uint32 cellHeight
            Dimension = dimension
            Cells = 
                let (cells: Cell[][]) = Array.zeroCreate (int (cellWidth * cellHeight) + 1)
                for cy in 0u..(cellHeight - 1u) do    // skip by cell heights
                    cells.[int cy] <- (makeRowBlocks dimension cellWidth cy rawImageBytes.Width rawImageBytes.Pixels)
                cells
         }

    // convenience helper utility methods (note that you can pass cX and cY as int, but if it wraps with high-bit set, it'll most likely cause unpredictable state)
    let getCell cX cY (cellImage: CellImage): Cell option =
        match (cX, cY) with
        | (_, y) when (int y) < 0 ->
            failwith (sprintf "Value (%i, %i) passed is negative" cX cY)
        | (x, _) when (int x) < 0 ->
            failwith (sprintf "Value (%i, %i) passed is negative" cX cY)
        | (x, y)  when (uint32 x) < cellImage.CellWidth && (uint32 y) < cellImage.CellHeight ->
            Some cellImage.Cells.[int x].[int y]
        | (_, y) when (uint y) > cellImage.CellHeight ->
            printfn "(%i, %i) - Y position is out of range (want less than %i)" cX cY cellImage.CellHeight
            None
        | (x, _) when (uint x) > cellImage.CellWidth ->
            printfn "(%i, %i) - X position is out of range (want less than %i)" cX cY cellImage.CellWidth
            None
        | (_, _) ->    // unsure why this case is needed, but it's complaining that all combinations aren't covered...
            failwith "Unhandled exception!"

    let dumpRGBA (image: RawImageRGB) =
        for y in 0u..(image.Height - 1u) do
            let scanLine = y * image.Width
            printf "%4i (width: %4i): " scanLine image.Width
            for x in 0u..(image.Width - 1u) do
                let pixel = image.Data.[int (x + scanLine)]
                printf "%08X " ((uint32 pixel.A <<< (8 * 3)) ||| (uint32 pixel.R <<< (8 * 2)) ||| (uint32 pixel.G <<< (8 * 1)) ||| (uint32 pixel.B <<< (8 * 0)))
            printfn ""
        printfn ""

    let dumpByteImage (image: RawImageBytes) =
        for y in 0u..(image.Height - 1u) do
            let scanLine = y * image.Width
            printf "%4i (width: %4i): " scanLine image.Width
            for x in 0u..(image.Width - 1u) do
                let pixel = image.Pixels.[int (x + scanLine)]
                printf "%02X " pixel
            printfn ""
        printfn ""

    let dumpCellImage (image: CellImage) =
        for hY in 0u..(image.CellHeight - 1u) do
            for cy in 0u..(image.Dimension - 1u) do
                printf "%4i (width: %4i): " hY image.CellWidth
                for wX in 0u..(image.CellWidth - 1u) do
                    let cell = image.Cells.[int hY].[int wX]
                    // within the cell, extract relative [0..Y][0..X]
                    for cx in 0u..(cell.Dimension - 1u) do
                        printf "{(%02i,%02i) %02X} " cy cx cell.Block.[int cy].[int cx]
                    printf " | "
                printfn ""
            printfn ""
        printfn ""
