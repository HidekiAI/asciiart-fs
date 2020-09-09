namespace libaafs

type RawImage = {
    Width: uint32
    Height: uint32
    RGBs: (byte * byte * byte) []
}
type GreyScaledImage = {
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
   let readPng filename: RawImage
       ()
   
   let toGreyScale (rawImage: RawImage): GreyScaledImage
       ()
   
   let toBlock dimension (greyScaledImage: GreyScaledImage): CellImage =
       ()
