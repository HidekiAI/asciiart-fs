namespace libaafs
open System
open System.Drawing

type RGBA = { R: byte; G: byte; B: byte; A: byte }
type RawImageRGB = {
    Width: uint32
    Height: uint32
    Data: RGBA []  // single stream, leave it to width and height to make it 2D
}
type RawImageBytes = {
    Width: uint32
    Height: uint32
    Pixel: byte[]   // only 16 shades of grey are visible to human eyes
}
type Cell = {
    Dimension: uint32    // i.e. 2x2, 4x4, 8x8, etc
    Block: byte[][]
}
type CellImage = {
    Width: uint32    // actual raw image dimension
    Height: uint32
    CellWidth: uint32   // i.e. on a 1024 pixels width, at 4x4, CellWidth would be 1024/4 = 256 cells wide
    CellHeight: uint32
    Cells: Cell[][]
}

module image =
    // there are no check to determine if filename has extension '.png', nor does it check (after reading) if it is Bitmap.Png type
    let private read (filename: string): RawImageRGB option =
        try
            let image = new Bitmap(filename) // IDisposable

            let imageArray =
                Array.zeroCreate (image.Width * image.Height) //  pre-allocate array block for performance (way faster than Array.Append! no leak, probably GC kicking in after 1/4 point, gets slower and slower)

            printfn "'%A': Width=%A, Height=%A" filename image.Width image.Height
            { Width = uint32 image.Width
              Height = uint32 image.Height
              Data =
                  for y in 0u .. uint32 (image.Height - 1) do
                      for x in 0u .. uint32 (image.Width - 1) do
                          let pixel: System.Drawing.Color = image.GetPixel(int x, int y)
                          imageArray.[int (x * y)] <- { R = pixel.R
                                                        G = pixel.G
                                                        B = pixel.B
                                                        A = pixel.A }
                  imageArray }
            |> Some
        with :? ArgumentException ->
            printfn "Cannot locate or open file '%A'" filename
            None

    let readPng filename: RawImageRGB =
        match read filename with
        | Some image -> image
        | None -> failwith "Unable to load %A" filename
   
    let toGreyScale (rawImage: RawImageRGB): RawImageBytes =
        let arraySize = int (rawImage.Width * rawImage.Height)
        let fromRGBA =
            fun rgba -> 
                byte ((rgba.R + rgba.G + rgba.B) / 3uy) &&& 255uy

        let (pixelArray: byte[]) = Array.zeroCreate arraySize
        {
            Width = rawImage.Width
            Height = rawImage.Height
            Pixel = 
                for pxy in 0..arraySize do
                    pixelArray.[pxy] <- fromRGBA rawImage.Data.[pxy]
                pixelArray
        }

    let private toImageRect width height (byteArray: byte[]): byte[][] =
        let twoDArray = Array.zeroCreate height
        for y in 0..height do
            let scanLine = Array.zeroCreate width
            for x in 0..width do
                scanLine.[x] <- byteArray.[x + (y * width)]
            twoDArray.[y] <- scanLine
        twoDArray

    let private makeRowBlocks dimension (cellWidth: uint32) (cy: uint32) (arrayedImage: byte[]): Cell[] =
        let cellRow = Array.zeroCreate (int cellWidth)
        for cx in 0u..cellWidth do
            // create a NxN cell block
            let cell = {
                Dimension = dimension; 
                Block = [|
                            for pyRelative in 0u..dimension do
                                let blockRow =
                                    [|
                                        for pxRelative in 0u..dimension do
                                            let px = int ((cx * dimension) + pxRelative)
                                            let py = int ((cy * dimension) + pyRelative)
                                            let pixel = arrayedImage.[px * py]
                                            pixel
                                    |]
                                blockRow
                        |] 
            }
            cellRow.[int cx] <- cell
        cellRow
   
    let toBlock dimension width height arrayedImage: CellImage =
        let cellWidth = width / dimension
        let cellHeight = height / dimension
        {
            Width = uint32 width
            Height = uint32 height
            CellWidth = uint32 cellWidth
            CellHeight = uint32 cellHeight
            Cells = 
                let (cells: Cell[][]) = Array.zeroCreate (cellWidth * cellHeight)
                for cy in 0u..cellHeight do
                    cells.[int cy] <- (makeRowBlocks dimension cellWidth cy arrayedImage)
                cells
         }
