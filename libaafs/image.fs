namespace libaafs

open System.Text

//open System.Data
//open System.Linq.Expressions

// Brief note about jagged [][] versus multidimensional [,] arrays; I'm using jagged[][] for performance, and will not be using Array2D.zeroCreate for that reason...

type RGBA = { R: byte; G: byte; B: byte; A: byte }

type RawImageRGB =
    { Width: uint32
      Height: uint32
      // Possibly use System.Drawing.Color here
      Data: RGBA [] } // single stream, leave it to width and height to make it 2D

type RawImageBytes =
    { Width: uint32
      Height: uint32
      Pixels: byte [] } // only 16 shades of grey are visible to human eyes

type Cell =
    { Dimension: uint32 // i.e. 2x2, 4x4, 8x8, etc
      Block: byte [] [] } // NOTE: it's [y][x]

type CellImage =
    { Width: uint32 // actual raw image dimension
      Height: uint32
      CellWidth: uint32 // i.e. on a 1024 pixels width, at 4x4, CellWidth would be 1024/4 = 256 cells wide
      CellHeight: uint32
      Dimension: uint32 // though this is embedded into Cell record, it's here so that you do not need to pick one of the Cells to lookup its dimensions
      Cells: Cell [] [] } // NOTE: it's [y][x]

module image =
    open System
    open System.Drawing

    // there are no check to determine if filename has extension '.png', nor does it check (after reading) if it is Bitmap.Png type
    let private read (filename: string): RawImageRGB option =
        try
            let image = new Bitmap(filename) // IDisposable

            // NOTE: This will initialize each cells with NULL, so if you get NULL, it's most likely because of programmer error, in
            // which you did not correctly cover each pixels (i.e. you missed out the edge of the image due to -1, etc)
            let arraySize = image.Height * (image.Width - 1) // why not Height-1?
            // odd, but need to allocate +1 here
            let imageArray = Array.zeroCreate (arraySize + 1) //  pre-allocate array block for performance (way faster than Array.Append! no leak, probably GC kicking in after 1/4 point, gets slower and slower)

            printfn
                "Opened and read file '%A': Width=%A, Height=%A (Size: %A pixels, ArraySize: %A bytes)"
                filename
                image.Width
                image.Height
                (image.Width * image.Height)
                imageArray.Length

            let retRecord =
                { Width = uint32 image.Width
                  Height = uint32 image.Height
                  Data =
                      for y in 0 .. (image.Height - 1) do
                          for x in 0 .. (image.Width - 1) do
                              let pixel (*System.Drawing.*) : Color = image.GetPixel(int x, int y)
                              imageArray.[x + ((image.Width - 1) * y)] <- { R = pixel.R
                                                                            G = pixel.G
                                                                            B = pixel.B
                                                                            A = pixel.A }
                      imageArray }
            // verify that last pixel was written (cannot really unit-test actual file reading, so this is here)

            match box retRecord.Data.[arraySize] with
            | null -> failwith "Unable to read entire image of dimensions specified"
            | v -> ignore v
            retRecord |> Some
        with
        | :? ArgumentOutOfRangeException ->
            printfn "Argument (either X or Y) for Bitmap.GetPixel(x,y) is out of range (programmer error)"
            None
        | :? ArgumentException ->
            printfn "ArgumentExceptions for '%A'" filename
            None
        | _ ->
            printfn "Unhandled exception"
            None

    let readPng filename: RawImageRGB =
        match read filename with
        | Some image -> image
        | None -> failwith (sprintf "Unable to load %A" filename)

    let toGreyScale (rawImage: RawImageRGB): RawImageBytes =
        printfn
            "Processing RawImageRGB: Width=%A, Height=%A, Size=%A bytes"
            rawImage.Width
            rawImage.Height
            rawImage.Data.Length
        // this method just takes percentages (33%) of each color
        let fromRGBA33 (rgba: RGBA) =
            byte (((int rgba.R) + (int rgba.G) + (int rgba.B)) / 3)
            &&& 255uy
        // divide by 16 parts, mask it with lower 2 bits, and shift to AARRGGBB bits
        let fromRGBA2Bits (rgba: RGBA) =
            byte
                ((((rgba.A >>> 4) &&& 3uy) <<< 6)
                 &&& (((rgba.R >>> 4) &&& 3uy) <<< 4)
                 &&& (((rgba.G >>> 4) &&& 3uy) <<< 2)
                 &&& (((rgba.B >>> 4) &&& 3uy) <<< 0))
            &&& 255uy
        // Similar to 2-bits RGBA, but without the alpha bits which makes it 00RRGGBB
        let fromRGB2Bits (rgba: RGBA) =
            byte
                (
                // NOTE: Some techniques bias 2 colors to be 3 bits
                (((rgba.R >>> 4) &&& 3uy) <<< 4)
                &&& (((rgba.G >>> 4) &&& 3uy) <<< 2)
                &&& (((rgba.B >>> 4) &&& 3uy) <<< 0))
            &&& 255uy

        let pixelArray =
            Array.zeroCreate (int (rawImage.Width * rawImage.Height))

        { Width = rawImage.Width
          Height = rawImage.Height
          Pixels =
              let arraySize = uint32 rawImage.Data.Length - 1u
              for pxy in 0u .. arraySize do
                  pixelArray.[int pxy] <- fromRGBA33 rawImage.Data.[int pxy]
              pixelArray }

    let private toImageRect width height (byteArray: byte []): byte [] [] =
        let twoDArray = Array.zeroCreate (height) // creating jagged[][] array, but initializing only the rows
        for y in 0 .. (height - 1) do
            let scanLine = Array.zeroCreate (width) // initialize the columns for each rows
            for x in 0 .. (width - 1) do
                scanLine.[x] <- byteArray.[x + (y * width)]
            twoDArray.[y] <- scanLine
        twoDArray

    let private copyRectToCell dimension pX pY stride (vector: byte []): Cell =
        if (uint32
                (((pY + (dimension - 1u)) * stride)
                 + (pX + (dimension - 1u)))) > (uint32 vector.Length) then
            failwith "Invalid (X,Y) coordinate, will lead to index outside the buffer pool"

        let retCell =
            { Dimension = dimension
              Block =
                  // performance-wise, rather than doing [| for ... |] to dynamically create an array, using
                  // Array.zeroCreate has been much more performant, especially when it gets called over and over; even
                  // if each block size is small, the repeated GC causes no memory to grow but rather gets hit over and
                  // over during sweeping...
                  //      //let block =
                  //      //    [| for pyRelative in 0u .. (cellDimension - 1u) do
                  //      //        let blockRow =
                  //      //            [| for pxRelative in 0u .. (cellDimension - 1u) do
                  //      //                let px = int ((cx * cellDimension) + pxRelative)
                  //      //                let py =
                  //      //                    int ((cellY * cellDimension) + pyRelative) // py / imageWidth = scanline
                  //      //                let pixelIndex = int ((uint32 py * stride) + uint32 px)
                  //      //                vector.[pixelIndex] |]
                  //      //        blockRow |]
                  //      //block
                  let block = Array.zeroCreate (int dimension)
                  for vY in pY .. (pY + (dimension - 1u)) do
                      let row = Array.zeroCreate (int dimension)
                      for vX in pX .. (pX + (dimension - 1u)) do
                          row.[int (vX - pX)] <- vector.[int ((vY * stride) + vX)]
                      block.[int (vY - pY)] <- row
                  block }
        retCell

    // Cannot assume `stride = (cellWidthCount * dimension)` since cellWidth truncates the right edge, but one can assume
    // that `stride >= (cellWidthCount * dimension)`
    let private makeCellRow (cellDimension: uint32)
                            (cellWidthCount: uint32) // number of cells in a row
                            (cellY: uint32) // cell Y position based on stride and vector; to calculate pixelY -> scanline = cellY * cellDimension
                            (stride: uint32) // for a single dimension vector, need to know where the edge of the image is
                            (vector: byte []) // entire image stream, there are no check when you pass cellY outside the image buffer
                            : Cell [] =
        if (cellDimension % 2u) = 1u then failwith "Dimension must be even sized"
        if stride < (cellWidthCount * cellDimension)
        then failwith "Cell count for the row cannot exceed the image width"

        let cellRow = Array.zeroCreate (int cellWidthCount)
        let scanline = cellY * cellDimension // upper left Y of the row we're creating

        for cx in 0u .. (cellWidthCount - 1u) do
            let pX = (cx * cellDimension) // upper left X of the current cell we're going to build
            // create a NxN cell block
            let cell =
                copyRectToCell cellDimension pX scanline stride vector

            cellRow.[int cx] <- cell
        cellRow

    let private cellToByteArray (cellBlock: byte [] []) =
        cellBlock |> Array.collect (fun row -> row) // TODO: Check if this gets reversed...

    // partitions a block into a quadrant, and inspect each quadrant for how much coverage it has, in which if that quad
    // covers more than 49%, make that quadrant all on with the averaged color value, else make it all off
    let private convertToBitFlaggedCell (cell: Cell): Cell =
        let toQuad (_cell: Cell) =
            let quadDimension = _cell.Dimension / 2u
            makeCellRow quadDimension 2u 0u _cell.Dimension (cellToByteArray _cell.Block)

        // strategy would be to look at each quads, and if the quad contains 2 or more pixels, mark that as all occupied
        let allMarked =
            [| for _ in 0u .. (cell.Dimension - 1u) do
                [| for _ in 0u .. (cell.Dimension - 1u) do
                    1uy |] |]

        let allCleared =
            [| for _ in 0u .. (cell.Dimension - 1u) do
                [| for _ in 0u .. (cell.Dimension - 1u) do
                    0uy |] |]

        let quaded = toQuad cell
        quaded
        |> Array.sumBy (fun blockRow ->
            cellToByteArray blockRow.Block
            |> Array.sumBy (fun b -> if b > 0uy then 1uy else 0uy))
        |> fun blockSum ->
            match blockSum with
            | v when v < (byte (cell.Dimension / 2u)) -> allCleared
            | _ -> allMarked
        |> fun block -> { cell with Block = block }

    // Cannot assume imageWidth = (cellWidth * dimension) since cellWidth truncates the right edge
    let private makeRowBlocks (dimension: uint32)
                              (cellWidth: uint32)
                              (cy: uint32)
                              (imageWidth: uint32)
                              (arrayedImage: byte [])
                              : Cell [] =
        if (dimension % 2u) = 1u then failwith "Dimension must be even sized"

        let cells =
            makeCellRow dimension cellWidth cy imageWidth arrayedImage

        cells

    let toBlock dimension (rawImageBytes: RawImageBytes): CellImage =
        if (dimension % 2u) = 1u then failwith "Dimension must be even sized"
        printfn
            "Processing RawImageBytes: Width=%A, Height=%A, Size=%A bytes"
            rawImageBytes.Width
            rawImageBytes.Height
            rawImageBytes.Pixels.Length
        let cellWidth = rawImageBytes.Width / dimension
        let cellHeight = rawImageBytes.Height / dimension
        //let arraySize = cellHeight * (cellWidth - 1u)
        let retCellImage =
            { Width = rawImageBytes.Width
              Height = rawImageBytes.Height
              CellWidth = uint32 cellWidth
              CellHeight = uint32 cellHeight
              Dimension = dimension
              Cells =
                  let cells = Array.zeroCreate (int cellHeight) // using jagged[][] array, and initialize the rows

                  for cy in 0u .. (cellHeight - 1u) do // skip by cell heights
                      cells.[int cy] <- (makeRowBlocks dimension cellWidth cy rawImageBytes.Width rawImageBytes.Pixels)
                  cells }
        retCellImage

    let getConvertedBlocks cellImage =
        { cellImage with
              Cells =
                  cellImage.Cells
                  |> Array.map (fun cellRow ->
                      cellRow
                      |> Array.map (fun cell -> convertToBitFlaggedCell cell)) }

    // convenience helper utility methods (note that you can pass cX and cY as int, but if it wraps with high-bit set, it'll most likely cause unpredictable state)
    let getCell cX cY (cellImage: CellImage): Cell option =
        match (cX, cY) with
        | (_, y) when (int y) < 0 -> failwith (sprintf "Value (%i, %i) passed is negative" cX cY)
        | (x, _) when (int x) < 0 -> failwith (sprintf "Value (%i, %i) passed is negative" cX cY)
        | (x, y) when (uint32 x) < cellImage.CellWidth
                      && (uint32 y) < cellImage.CellHeight -> Some cellImage.Cells.[int x].[int y]
        | (_, y) when (uint32 y) > cellImage.CellHeight ->
            printfn "(%i, %i) - Y position is out of range (want less than %i)" cX cY cellImage.CellHeight
            None
        | (x, _) when (uint32 x) > cellImage.CellWidth ->
            printfn "(%i, %i) - X position is out of range (want less than %i)" cX cY cellImage.CellWidth
            None
        | (_, _) -> failwith "Unhandled exception!"

    let dumpRGBA (sb: StringBuilder) (image: RawImageRGB) =
        sb.AppendLine(sprintf "RawImageRGB Width: %A, Height: %A, Size: %A" image.Width image.Height image.Data.Length)
        |> ignore
        for y in 0u .. (image.Height - 1u) do
            let scanLine = y * image.Width
            sb.Append(sprintf "%4i (width: %4i): " scanLine image.Width)
            |> ignore
            for x in 0u .. (image.Width - 1u) do
                let pixel = image.Data.[int (x + scanLine)]
                sb.Append
                    (sprintf
                        "%08X "
                         ((uint32 pixel.A <<< (8 * 3))
                          ||| (uint32 pixel.R <<< (8 * 2))
                          ||| (uint32 pixel.G <<< (8 * 1))
                          ||| (uint32 pixel.B <<< (8 * 0))))
                |> ignore
            sb.AppendLine(sprintf "") |> ignore
        sb.AppendLine(sprintf "") |> ignore

    let dumpByteImage (sb: StringBuilder) (image: RawImageBytes) =
        sb.AppendLine
            (sprintf "RawImageBytes Width: %A, Height: %A, Size: %A" image.Width image.Height image.Pixels.Length)
        |> ignore
        for y in 0u .. (image.Height - 1u) do
            let scanLine = y * image.Width
            sb.Append(sprintf "%4i (width: %4i): " scanLine image.Width)
            |> ignore
            for x in 0u .. (image.Width - 1u) do
                let pixel = image.Pixels.[int (x + scanLine)]
                sb.Append(sprintf "%02X " pixel) |> ignore
            sb.AppendLine(sprintf "") |> ignore
        sb.AppendLine(sprintf "") |> ignore

    let dumpCellImage (sb: StringBuilder) (image: CellImage) =
        sb.AppendLine
            (sprintf
                "CellImage Width: %A, Height: %A, CellWidth: %A, CellHeight: %A, Dimension: %A Size: %A"
                 image.Width
                 image.Height
                 image.CellWidth
                 image.CellHeight
                 image.Dimension
                 image.Cells.Length)
        |> ignore
        for hY in 0u .. (image.CellHeight - 1u) do
            for cy in 0u .. (image.Dimension - 1u) do
                sb.Append(sprintf "%4i (width: %4i): " hY image.CellWidth)
                |> ignore
                for wX in 0u .. (image.CellWidth - 1u) do
                    let cell = image.Cells.[int hY].[int wX]
                    // within the cell, extract relative [0..Y][0..X]
                    for cx in 0u .. (cell.Dimension - 1u) do
                        sb.Append(sprintf "{(%02i,%02i) %02X} " cy cx cell.Block.[int cy].[int cx])
                        |> ignore
                    sb.Append(sprintf " | ") |> ignore
                sb.AppendLine(sprintf "") |> ignore
            sb.AppendLine(sprintf "") |> ignore
        sb.AppendLine(sprintf "") |> ignore
