namespace Aafs

type Block =
    { WidthAndHeight: int // it's square, so all we need is one
      Data: byte [] [] // usualy NxN (square) dimension block
      Char: char }

module Map =

    let hello name = printfn "Hello %s" name

    // 4x4 pixel to be mapped to text
    let map4x4 =
        [ { WidthAndHeight = 4
            Data = [| [| 0uy; 0uy |]; [| 0uy; 0uy |] |]
            Char = ' ' }
          { WidthAndHeight = 4
            Data = [| [| 0uy; 0uy |]; [| 0uy; 1uy |] |]
            Char = '.' }
          { WidthAndHeight = 4
            Data = [| [| 0uy; 0uy |]; [| 1uy; 0uy |] |]
            Char = ',' }
          { WidthAndHeight = 4
            Data = [| [| 0uy; 0uy |]; [| 1uy; 1uy |] |]
            Char = '_' }
          { WidthAndHeight = 4
            Data = [| [| 0uy; 1uy |]; [| 0uy; 0uy |] |]
            Char = '\'' }
          { WidthAndHeight = 4
            Data = [| [| 0uy; 1uy |]; [| 0uy; 1uy |] |]
            Char = '|' }
          { WidthAndHeight = 4
            Data = [| [| 0uy; 1uy |]; [| 1uy; 0uy |] |]
            Char = '/' }
          { WidthAndHeight = 4
            Data = [| [| 0uy; 1uy |]; [| 1uy; 1uy |] |]
            Char = '6' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 0uy |]; [| 0uy; 0uy |] |]
            Char = '`' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 0uy |]; [| 0uy; 1uy |] |]
            Char = '\\' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 0uy |]; [| 1uy; 0uy |] |]
            Char = '!' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 0uy |]; [| 1uy; 1uy |] |]
            Char = 'L' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 1uy |]; [| 0uy; 0uy |] |]
            Char = '-' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 1uy |]; [| 0uy; 1uy |] |]
            Char = ']' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 1uy |]; [| 1uy; 0uy |] |]
            Char = 'T' }
          { WidthAndHeight = 4
            Data = [| [| 1uy; 1uy |]; [| 1uy; 1uy |] |]
            Char = '#' } ]
        |> Seq.groupBy (fun dat -> dat.WidthAndHeight)
        |> Seq.map (fun group ->
            group
            group
            |> Seq.map (fun v ->
                |> Seq.map (fun v ->
                    v
                    |> dict)
                )
