namespace libaafs
open System.Drawing

type RGBA = { R: byte; G: byte; B: byte; A: byte }
type RawImageRGB = {
    Width: uint32
    Height: uint32
      Data: RGBA []  // single stream, leave it to width and height to make it 2D
}
type RawImageByte = {
    Width: uint32
    Height: uint32
    Pixel: byte    // only 16 shades of grey are visible to human eyes
}
type Cell = {
    Dimension: uint32    // i.e. 2x2, 4x4, 8x8, etc
    Block: byte[][]
}
type CellImage = {
    Width: uint32    // actual raw image dimension
    Height: uint32
    Dimension: uint32    // though each Cell would have dimension, this is global sanity check that must match the Cell block dimensions
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

   let readPng filename: RawImageRGB
       ()
   
   let toGreyScale (rawImage: RawImage): RawImageByte 
       ()
   
   let toBlock dimension (greyScaledImage: GreyScaledImage): CellImage =
       ()
