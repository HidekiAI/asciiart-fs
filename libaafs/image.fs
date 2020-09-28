namespace libaafs

open System.Text

//open System.Data
//open System.Linq.Expressions

// Brief note about jagged [][] versus multidimensional [,] arrays; I'm using jagged[][] for performance, and will not be using Array2D.zeroCreate for that reason...

type RGBA = { R: byte; G: byte; B: byte; A: byte }

type HTMLColor = string
type CompressedColor = byte

type ASCIIColor = string

type Pixel =
    { Compressed: CompressedColor
      Color: RGBA
      HtmlColor: HTMLColor
      ASCIIColor: ASCIIColor }

type RawImageRGB =
    { Width: uint32
      Height: uint32
      // Possibly use System.Drawing.Color here
      Data: RGBA [] } // single stream, leave it to width and height to make it 2D

type RawLibPixelImage =
    { Width: uint32
      Height: uint32
      Pixels: Pixel [] } // only 16 shades of grey are visible to human eyes

type Cell =
    { Dimension: uint32 // i.e. 2x2, 4x4, 8x8, etc
      Block: Pixel [] [] } // NOTE: it's [y][x]

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

    let HtmlToASCIIColor (htmlColor: HTMLColor): string =
        // for code in {0..255}; do echo -e "\e[38;05;${code}m $code"; done
//#region colorTable
        let colorLookup =
            [| {| HtmlColor = "CC0000"
                  ASCIICode = "\e[38;05;1m" |}
               {| HtmlColor = "4E9A06"
                  ASCIICode = "\e[38;05;2m" |}
               {| HtmlColor = "C4A000"
                  ASCIICode = "\e[38;05;3m" |}
               {| HtmlColor = "3465A4"
                  ASCIICode = "\e[38;05;4m" |}
               {| HtmlColor = "75507B"
                  ASCIICode = "\e[38;05;5m" |}
               {| HtmlColor = "06989A"
                  ASCIICode = "\e[38;05;6m" |}
               {| HtmlColor = "D3D7CF"
                  ASCIICode = "\e[38;05;7m" |}
               {| HtmlColor = "555753"
                  ASCIICode = "\e[38;05;8m" |}
               {| HtmlColor = "EF2929"
                  ASCIICode = "\e[38;05;9m" |}
               {| HtmlColor = "8AE234"
                  ASCIICode = "\e[38;05;10m" |}
               {| HtmlColor = "FCE94F"
                  ASCIICode = "\e[38;05;11m" |}
               {| HtmlColor = "729FCF"
                  ASCIICode = "\e[38;05;12m" |}
               {| HtmlColor = "AD7FA8"
                  ASCIICode = "\e[38;05;13m" |}
               {| HtmlColor = "34E2E2"
                  ASCIICode = "\e[38;05;14m" |}
               {| HtmlColor = "EEEEEC"
                  ASCIICode = "\e[38;05;15m" |}
               {| HtmlColor = "000000"
                  ASCIICode = "\e[38;05;16m" |}
               {| HtmlColor = "00005F"
                  ASCIICode = "\e[38;05;17m" |}
               {| HtmlColor = "000087"
                  ASCIICode = "\e[38;05;18m" |}
               {| HtmlColor = "0000AF"
                  ASCIICode = "\e[38;05;19m" |}
               {| HtmlColor = "0000D7"
                  ASCIICode = "\e[38;05;20m" |}
               {| HtmlColor = "0000FF"
                  ASCIICode = "\e[38;05;21m" |}
               {| HtmlColor = "005F00"
                  ASCIICode = "\e[38;05;22m" |}
               {| HtmlColor = "005F5F"
                  ASCIICode = "\e[38;05;23m" |}
               {| HtmlColor = "005F87"
                  ASCIICode = "\e[38;05;24m" |}
               {| HtmlColor = "005FAF"
                  ASCIICode = "\e[38;05;25m" |}
               {| HtmlColor = "005FD7"
                  ASCIICode = "\e[38;05;26m" |}
               {| HtmlColor = "005FFF"
                  ASCIICode = "\e[38;05;27m" |}
               {| HtmlColor = "008700"
                  ASCIICode = "\e[38;05;28m" |}
               {| HtmlColor = "00875F"
                  ASCIICode = "\e[38;05;29m" |}
               {| HtmlColor = "008787"
                  ASCIICode = "\e[38;05;30m" |}
               {| HtmlColor = "0087AF"
                  ASCIICode = "\e[38;05;31m" |}
               {| HtmlColor = "0087D7"
                  ASCIICode = "\e[38;05;32m" |}
               {| HtmlColor = "0087FF"
                  ASCIICode = "\e[38;05;33m" |}
               {| HtmlColor = "00AF00"
                  ASCIICode = "\e[38;05;34m" |}
               {| HtmlColor = "00AF5F"
                  ASCIICode = "\e[38;05;35m" |}
               {| HtmlColor = "00AF87"
                  ASCIICode = "\e[38;05;36m" |}
               {| HtmlColor = "00AFAF"
                  ASCIICode = "\e[38;05;37m" |}
               {| HtmlColor = "00AFD7"
                  ASCIICode = "\e[38;05;38m" |}
               {| HtmlColor = "00AFFF"
                  ASCIICode = "\e[38;05;39m" |}
               {| HtmlColor = "00D700"
                  ASCIICode = "\e[38;05;40m" |}
               {| HtmlColor = "00D75F"
                  ASCIICode = "\e[38;05;41m" |}
               {| HtmlColor = "00D787"
                  ASCIICode = "\e[38;05;42m" |}
               {| HtmlColor = "00D7AF"
                  ASCIICode = "\e[38;05;43m" |}
               {| HtmlColor = "00D7D7"
                  ASCIICode = "\e[38;05;44m" |}
               {| HtmlColor = "00D7FF"
                  ASCIICode = "\e[38;05;45m" |}
               {| HtmlColor = "00FF00"
                  ASCIICode = "\e[38;05;46m" |}
               {| HtmlColor = "00FF5F"
                  ASCIICode = "\e[38;05;47m" |}
               {| HtmlColor = "00FF87"
                  ASCIICode = "\e[38;05;48m" |}
               {| HtmlColor = "00FFAF"
                  ASCIICode = "\e[38;05;49m" |}
               {| HtmlColor = "00FFD7"
                  ASCIICode = "\e[38;05;50m" |}
               {| HtmlColor = "00FFFF"
                  ASCIICode = "\e[38;05;51m" |}
               {| HtmlColor = "5F0000"
                  ASCIICode = "\e[38;05;52m" |}
               {| HtmlColor = "5F005F"
                  ASCIICode = "\e[38;05;53m" |}
               {| HtmlColor = "5F0087"
                  ASCIICode = "\e[38;05;54m" |}
               {| HtmlColor = "5F00AF"
                  ASCIICode = "\e[38;05;55m" |}
               {| HtmlColor = "5F00D7"
                  ASCIICode = "\e[38;05;56m" |}
               {| HtmlColor = "5F00FF"
                  ASCIICode = "\e[38;05;57m" |}
               {| HtmlColor = "5F5F00"
                  ASCIICode = "\e[38;05;58m" |}
               {| HtmlColor = "5F5F5F"
                  ASCIICode = "\e[38;05;59m" |}
               {| HtmlColor = "5F5F87"
                  ASCIICode = "\e[38;05;60m" |}
               {| HtmlColor = "5F5FAF"
                  ASCIICode = "\e[38;05;61m" |}
               {| HtmlColor = "5F5FD7"
                  ASCIICode = "\e[38;05;62m" |}
               {| HtmlColor = "5F5FFF"
                  ASCIICode = "\e[38;05;63m" |}
               {| HtmlColor = "5F8700"
                  ASCIICode = "\e[38;05;64m" |}
               {| HtmlColor = "5F875F"
                  ASCIICode = "\e[38;05;65m" |}
               {| HtmlColor = "5F8787"
                  ASCIICode = "\e[38;05;66m" |}
               {| HtmlColor = "5F87AF"
                  ASCIICode = "\e[38;05;67m" |}
               {| HtmlColor = "5F87D7"
                  ASCIICode = "\e[38;05;68m" |}
               {| HtmlColor = "5F87FF"
                  ASCIICode = "\e[38;05;69m" |}
               {| HtmlColor = "5FAF00"
                  ASCIICode = "\e[38;05;70m" |}
               {| HtmlColor = "5FAF5F"
                  ASCIICode = "\e[38;05;71m" |}
               {| HtmlColor = "5FAF87"
                  ASCIICode = "\e[38;05;72m" |}
               {| HtmlColor = "5FAFAF"
                  ASCIICode = "\e[38;05;73m" |}
               {| HtmlColor = "5FAFD7"
                  ASCIICode = "\e[38;05;74m" |}
               {| HtmlColor = "5FAFFF"
                  ASCIICode = "\e[38;05;75m" |}
               {| HtmlColor = "5FD700"
                  ASCIICode = "\e[38;05;76m" |}
               {| HtmlColor = "5FD75F"
                  ASCIICode = "\e[38;05;77m" |}
               {| HtmlColor = "5FD787"
                  ASCIICode = "\e[38;05;78m" |}
               {| HtmlColor = "5FD7AF"
                  ASCIICode = "\e[38;05;79m" |}
               {| HtmlColor = "5FD7D7"
                  ASCIICode = "\e[38;05;80m" |}
               {| HtmlColor = "5FD7FF"
                  ASCIICode = "\e[38;05;81m" |}
               {| HtmlColor = "5FFF00"
                  ASCIICode = "\e[38;05;82m" |}
               {| HtmlColor = "5FFF5F"
                  ASCIICode = "\e[38;05;83m" |}
               {| HtmlColor = "5FFF87"
                  ASCIICode = "\e[38;05;84m" |}
               {| HtmlColor = "5FFFAF"
                  ASCIICode = "\e[38;05;85m" |}
               {| HtmlColor = "5FFFD7"
                  ASCIICode = "\e[38;05;86m" |}
               {| HtmlColor = "5FFFFF"
                  ASCIICode = "\e[38;05;87m" |}
               {| HtmlColor = "870000"
                  ASCIICode = "\e[38;05;88m" |}
               {| HtmlColor = "87005F"
                  ASCIICode = "\e[38;05;89m" |}
               {| HtmlColor = "870087"
                  ASCIICode = "\e[38;05;90m" |}
               {| HtmlColor = "8700AF"
                  ASCIICode = "\e[38;05;91m" |}
               {| HtmlColor = "8700D7"
                  ASCIICode = "\e[38;05;92m" |}
               {| HtmlColor = "8700FF"
                  ASCIICode = "\e[38;05;93m" |}
               {| HtmlColor = "875F00"
                  ASCIICode = "\e[38;05;94m" |}
               {| HtmlColor = "875F5F"
                  ASCIICode = "\e[38;05;95m" |}
               {| HtmlColor = "875F87"
                  ASCIICode = "\e[38;05;96m" |}
               {| HtmlColor = "875FAF"
                  ASCIICode = "\e[38;05;97m" |}
               {| HtmlColor = "875FD7"
                  ASCIICode = "\e[38;05;98m" |}
               {| HtmlColor = "875FFF"
                  ASCIICode = "\e[38;05;99m" |}
               {| HtmlColor = "878700"
                  ASCIICode = "\e[38;05;100m" |}
               {| HtmlColor = "87875F"
                  ASCIICode = "\e[38;05;101m" |}
               {| HtmlColor = "878787"
                  ASCIICode = "\e[38;05;102m" |}
               {| HtmlColor = "8787AF"
                  ASCIICode = "\e[38;05;103m" |}
               {| HtmlColor = "8787D7"
                  ASCIICode = "\e[38;05;104m" |}
               {| HtmlColor = "8787FF"
                  ASCIICode = "\e[38;05;105m" |}
               {| HtmlColor = "87AF00"
                  ASCIICode = "\e[38;05;106m" |}
               {| HtmlColor = "87AF5F"
                  ASCIICode = "\e[38;05;107m" |}
               {| HtmlColor = "87AF87"
                  ASCIICode = "\e[38;05;108m" |}
               {| HtmlColor = "87AFAF"
                  ASCIICode = "\e[38;05;109m" |}
               {| HtmlColor = "87AFD7"
                  ASCIICode = "\e[38;05;110m" |}
               {| HtmlColor = "87AFFF"
                  ASCIICode = "\e[38;05;111m" |}
               {| HtmlColor = "87D700"
                  ASCIICode = "\e[38;05;112m" |}
               {| HtmlColor = "87D75F"
                  ASCIICode = "\e[38;05;113m" |}
               {| HtmlColor = "87D787"
                  ASCIICode = "\e[38;05;114m" |}
               {| HtmlColor = "87D7AF"
                  ASCIICode = "\e[38;05;115m" |}
               {| HtmlColor = "87D7D7"
                  ASCIICode = "\e[38;05;116m" |}
               {| HtmlColor = "87D7FF"
                  ASCIICode = "\e[38;05;117m" |}
               {| HtmlColor = "87FF00"
                  ASCIICode = "\e[38;05;118m" |}
               {| HtmlColor = "87FF5F"
                  ASCIICode = "\e[38;05;119m" |}
               {| HtmlColor = "87FF87"
                  ASCIICode = "\e[38;05;120m" |}
               {| HtmlColor = "87FFAF"
                  ASCIICode = "\e[38;05;121m" |}
               {| HtmlColor = "87FFD7"
                  ASCIICode = "\e[38;05;122m" |}
               {| HtmlColor = "87FFFF"
                  ASCIICode = "\e[38;05;123m" |}
               {| HtmlColor = "AF0000"
                  ASCIICode = "\e[38;05;124m" |}
               {| HtmlColor = "AF005F"
                  ASCIICode = "\e[38;05;125m" |}
               {| HtmlColor = "AF0087"
                  ASCIICode = "\e[38;05;126m" |}
               {| HtmlColor = "AF00AF"
                  ASCIICode = "\e[38;05;127m" |}
               {| HtmlColor = "AF00D7"
                  ASCIICode = "\e[38;05;128m" |}
               {| HtmlColor = "AF00FF"
                  ASCIICode = "\e[38;05;129m" |}
               {| HtmlColor = "AF5F00"
                  ASCIICode = "\e[38;05;130m" |}
               {| HtmlColor = "AF5F5F"
                  ASCIICode = "\e[38;05;131m" |}
               {| HtmlColor = "AF5F87"
                  ASCIICode = "\e[38;05;132m" |}
               {| HtmlColor = "AF5FAF"
                  ASCIICode = "\e[38;05;133m" |}
               {| HtmlColor = "AF5FD7"
                  ASCIICode = "\e[38;05;134m" |}
               {| HtmlColor = "AF5FFF"
                  ASCIICode = "\e[38;05;135m" |}
               {| HtmlColor = "AF8700"
                  ASCIICode = "\e[38;05;136m" |}
               {| HtmlColor = "AF875F"
                  ASCIICode = "\e[38;05;137m" |}
               {| HtmlColor = "AF8787"
                  ASCIICode = "\e[38;05;138m" |}
               {| HtmlColor = "AF87AF"
                  ASCIICode = "\e[38;05;139m" |}
               {| HtmlColor = "AF87D7"
                  ASCIICode = "\e[38;05;140m" |}
               {| HtmlColor = "AF87FF"
                  ASCIICode = "\e[38;05;141m" |}
               {| HtmlColor = "AFAF00"
                  ASCIICode = "\e[38;05;142m" |}
               {| HtmlColor = "AFAF5F"
                  ASCIICode = "\e[38;05;143m" |}
               {| HtmlColor = "AFAF87"
                  ASCIICode = "\e[38;05;144m" |}
               {| HtmlColor = "AFAFAF"
                  ASCIICode = "\e[38;05;145m" |}
               {| HtmlColor = "AFAFD7"
                  ASCIICode = "\e[38;05;146m" |}
               {| HtmlColor = "AFAFFF"
                  ASCIICode = "\e[38;05;147m" |}
               {| HtmlColor = "AFD700"
                  ASCIICode = "\e[38;05;148m" |}
               {| HtmlColor = "AFD75F"
                  ASCIICode = "\e[38;05;149m" |}
               {| HtmlColor = "AFD787"
                  ASCIICode = "\e[38;05;150m" |}
               {| HtmlColor = "AFD7AF"
                  ASCIICode = "\e[38;05;151m" |}
               {| HtmlColor = "AFD7D7"
                  ASCIICode = "\e[38;05;152m" |}
               {| HtmlColor = "AFD7FF"
                  ASCIICode = "\e[38;05;153m" |}
               {| HtmlColor = "AFFF00"
                  ASCIICode = "\e[38;05;154m" |}
               {| HtmlColor = "AFFF5F"
                  ASCIICode = "\e[38;05;155m" |}
               {| HtmlColor = "AFFF87"
                  ASCIICode = "\e[38;05;156m" |}
               {| HtmlColor = "AFFFAF"
                  ASCIICode = "\e[38;05;157m" |}
               {| HtmlColor = "AFFFD7"
                  ASCIICode = "\e[38;05;158m" |}
               {| HtmlColor = "AFFFFF"
                  ASCIICode = "\e[38;05;159m" |}
               {| HtmlColor = "D70000"
                  ASCIICode = "\e[38;05;160m" |}
               {| HtmlColor = "D7005F"
                  ASCIICode = "\e[38;05;161m" |}
               {| HtmlColor = "D70087"
                  ASCIICode = "\e[38;05;162m" |}
               {| HtmlColor = "D700AF"
                  ASCIICode = "\e[38;05;163m" |}
               {| HtmlColor = "D700D7"
                  ASCIICode = "\e[38;05;164m" |}
               {| HtmlColor = "D700FF"
                  ASCIICode = "\e[38;05;165m" |}
               {| HtmlColor = "D75F00"
                  ASCIICode = "\e[38;05;166m" |}
               {| HtmlColor = "D75F5F"
                  ASCIICode = "\e[38;05;167m" |}
               {| HtmlColor = "D75F87"
                  ASCIICode = "\e[38;05;168m" |}
               {| HtmlColor = "D75FAF"
                  ASCIICode = "\e[38;05;169m" |}
               {| HtmlColor = "D75FD7"
                  ASCIICode = "\e[38;05;170m" |}
               {| HtmlColor = "D75FFF"
                  ASCIICode = "\e[38;05;171m" |}
               {| HtmlColor = "D78700"
                  ASCIICode = "\e[38;05;172m" |}
               {| HtmlColor = "D7875F"
                  ASCIICode = "\e[38;05;173m" |}
               {| HtmlColor = "D78787"
                  ASCIICode = "\e[38;05;174m" |}
               {| HtmlColor = "D787AF"
                  ASCIICode = "\e[38;05;175m" |}
               {| HtmlColor = "D787D7"
                  ASCIICode = "\e[38;05;176m" |}
               {| HtmlColor = "D787FF"
                  ASCIICode = "\e[38;05;177m" |}
               {| HtmlColor = "D7AF00"
                  ASCIICode = "\e[38;05;178m" |}
               {| HtmlColor = "D7AF5F"
                  ASCIICode = "\e[38;05;179m" |}
               {| HtmlColor = "D7AF87"
                  ASCIICode = "\e[38;05;180m" |}
               {| HtmlColor = "D7AFAF"
                  ASCIICode = "\e[38;05;181m" |}
               {| HtmlColor = "D7AFD7"
                  ASCIICode = "\e[38;05;182m" |}
               {| HtmlColor = "D7AFFF"
                  ASCIICode = "\e[38;05;183m" |}
               {| HtmlColor = "D7D700"
                  ASCIICode = "\e[38;05;184m" |}
               {| HtmlColor = "D7D75F"
                  ASCIICode = "\e[38;05;185m" |}
               {| HtmlColor = "D7D787"
                  ASCIICode = "\e[38;05;186m" |}
               {| HtmlColor = "D7D7AF"
                  ASCIICode = "\e[38;05;187m" |}
               {| HtmlColor = "D7D7D7"
                  ASCIICode = "\e[38;05;188m" |}
               {| HtmlColor = "D7D7FF"
                  ASCIICode = "\e[38;05;189m" |}
               {| HtmlColor = "D7FF00"
                  ASCIICode = "\e[38;05;190m" |}
               {| HtmlColor = "D7FF5F"
                  ASCIICode = "\e[38;05;191m" |}
               {| HtmlColor = "D7FF87"
                  ASCIICode = "\e[38;05;192m" |}
               {| HtmlColor = "D7FFAF"
                  ASCIICode = "\e[38;05;193m" |}
               {| HtmlColor = "D7FFD7"
                  ASCIICode = "\e[38;05;194m" |}
               {| HtmlColor = "D7FFFF"
                  ASCIICode = "\e[38;05;195m" |}
               {| HtmlColor = "FF0000"
                  ASCIICode = "\e[38;05;196m" |}
               {| HtmlColor = "FF005F"
                  ASCIICode = "\e[38;05;197m" |}
               {| HtmlColor = "FF0087"
                  ASCIICode = "\e[38;05;198m" |}
               {| HtmlColor = "FF00AF"
                  ASCIICode = "\e[38;05;199m" |}
               {| HtmlColor = "FF00D7"
                  ASCIICode = "\e[38;05;200m" |}
               {| HtmlColor = "FF00FF"
                  ASCIICode = "\e[38;05;201m" |}
               {| HtmlColor = "FF5F00"
                  ASCIICode = "\e[38;05;202m" |}
               {| HtmlColor = "FF5F5F"
                  ASCIICode = "\e[38;05;203m" |}
               {| HtmlColor = "FF5F87"
                  ASCIICode = "\e[38;05;204m" |}
               {| HtmlColor = "FF5FAF"
                  ASCIICode = "\e[38;05;205m" |}
               {| HtmlColor = "FF5FD7"
                  ASCIICode = "\e[38;05;206m" |}
               {| HtmlColor = "FF5FFF"
                  ASCIICode = "\e[38;05;207m" |}
               {| HtmlColor = "FF8700"
                  ASCIICode = "\e[38;05;208m" |}
               {| HtmlColor = "FF875F"
                  ASCIICode = "\e[38;05;209m" |}
               {| HtmlColor = "FF8787"
                  ASCIICode = "\e[38;05;210m" |}
               {| HtmlColor = "FF87AF"
                  ASCIICode = "\e[38;05;211m" |}
               {| HtmlColor = "FF87D7"
                  ASCIICode = "\e[38;05;212m" |}
               {| HtmlColor = "FF87FF"
                  ASCIICode = "\e[38;05;213m" |}
               {| HtmlColor = "FFAF00"
                  ASCIICode = "\e[38;05;214m" |}
               {| HtmlColor = "FFAF5F"
                  ASCIICode = "\e[38;05;215m" |}
               {| HtmlColor = "FFAF87"
                  ASCIICode = "\e[38;05;216m" |}
               {| HtmlColor = "FFAFAF"
                  ASCIICode = "\e[38;05;217m" |}
               {| HtmlColor = "FFAFD7"
                  ASCIICode = "\e[38;05;218m" |}
               {| HtmlColor = "FFAFFF"
                  ASCIICode = "\e[38;05;219m" |}
               {| HtmlColor = "FFD700"
                  ASCIICode = "\e[38;05;220m" |}
               {| HtmlColor = "FFD75F"
                  ASCIICode = "\e[38;05;221m" |}
               {| HtmlColor = "FFD787"
                  ASCIICode = "\e[38;05;222m" |}
               {| HtmlColor = "FFD7AF"
                  ASCIICode = "\e[38;05;223m" |}
               {| HtmlColor = "FFD7D7"
                  ASCIICode = "\e[38;05;224m" |}
               {| HtmlColor = "FFD7FF"
                  ASCIICode = "\e[38;05;225m" |}
               {| HtmlColor = "FFFF00"
                  ASCIICode = "\e[38;05;226m" |}
               {| HtmlColor = "FFFF5F"
                  ASCIICode = "\e[38;05;227m" |}
               {| HtmlColor = "FFFF87"
                  ASCIICode = "\e[38;05;228m" |}
               {| HtmlColor = "FFFFAF"
                  ASCIICode = "\e[38;05;229m" |}
               {| HtmlColor = "FFFFD7"
                  ASCIICode = "\e[38;05;230m" |}
               {| HtmlColor = "FFFFFF"
                  ASCIICode = "\e[38;05;231m" |}
               {| HtmlColor = "080808"
                  ASCIICode = "\e[38;05;232m" |}
               {| HtmlColor = "121212"
                  ASCIICode = "\e[38;05;233m" |}
               {| HtmlColor = "1C1C1C"
                  ASCIICode = "\e[38;05;234m" |}
               {| HtmlColor = "262626"
                  ASCIICode = "\e[38;05;235m" |}
               {| HtmlColor = "303030"
                  ASCIICode = "\e[38;05;236m" |}
               {| HtmlColor = "3A3A3A"
                  ASCIICode = "\e[38;05;237m" |}
               {| HtmlColor = "444444"
                  ASCIICode = "\e[38;05;238m" |}
               {| HtmlColor = "4E4E4E"
                  ASCIICode = "\e[38;05;239m" |}
               {| HtmlColor = "585858"
                  ASCIICode = "\e[38;05;240m" |}
               {| HtmlColor = "626262"
                  ASCIICode = "\e[38;05;241m" |}
               {| HtmlColor = "6C6C6C"
                  ASCIICode = "\e[38;05;242m" |}
               {| HtmlColor = "767676"
                  ASCIICode = "\e[38;05;243m" |}
               {| HtmlColor = "808080"
                  ASCIICode = "\e[38;05;244m" |}
               {| HtmlColor = "8A8A8A"
                  ASCIICode = "\e[38;05;245m" |}
               {| HtmlColor = "949494"
                  ASCIICode = "\e[38;05;246m" |}
               {| HtmlColor = "9E9E9E"
                  ASCIICode = "\e[38;05;247m" |}
               {| HtmlColor = "A8A8A8"
                  ASCIICode = "\e[38;05;248m" |}
               {| HtmlColor = "B2B2B2"
                  ASCIICode = "\e[38;05;249m" |}
               {| HtmlColor = "BCBCBC"
                  ASCIICode = "\e[38;05;250m" |}
               {| HtmlColor = "C6C6C6"
                  ASCIICode = "\e[38;05;251m" |}
               {| HtmlColor = "D0D0D0"
                  ASCIICode = "\e[38;05;252m" |}
               {| HtmlColor = "DADADA"
                  ASCIICode = "\e[38;05;253m" |}
               {| HtmlColor = "E4E4E4"
                  ASCIICode = "\e[38;05;254m" |}
               {| HtmlColor = "EEEEEE"
                  ASCIICode = "\e[38;05;255m" |} |]
        //#endregion

        let colorBlack =
            colorLookup
            |> Array.find (fun element -> element.HtmlColor = "000000") // if it cannot find 000000, it's a bug!
            |> fun element -> element.ASCIICode

        colorLookup
        |> Array.tryFind (fun element -> element.HtmlColor = htmlColor)
        |> fun element ->
            match element with
            | Some e -> e.ASCIICode
            | _ -> colorBlack

    let RGBAToHtml rgba =
        sprintf "%02X%02X%02X" rgba.R rgba.G rgba.B

    let avgPixel pixel1 pixel2 =
        let avgRGB =
            { R = (pixel1.Color.R + pixel2.Color.R) / 2uy
              G = (pixel1.Color.G + pixel2.Color.G) / 2uy
              B = (pixel1.Color.B + pixel2.Color.B) / 2uy
              A = (pixel1.Color.A + pixel2.Color.A) / 2uy }

        let htmlColor = RGBAToHtml avgRGB

        { Compressed = (pixel1.Compressed + pixel2.Compressed) / 2uy
          Color = avgRGB
          HtmlColor = htmlColor
          ASCIIColor = HtmlToASCIIColor htmlColor }

    let makePixel rgba =
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

        { Compressed = fromRGBA33 rgba
          Color = rgba
          HtmlColor = RGBAToHtml rgba
          ASCIIColor = HtmlToASCIIColor(RGBAToHtml rgba) }

    let makeBlackPixel =
        makePixel { R = 0uy; G = 0uy; B = 0uy; A = 0uy }

    // there are no check to determine if filename has extension '.png', nor does it check (after reading) if it is Bitmap.Png type
    // WARNING: Unsure if it's a bug or by design, but when reading (at least PNG) image, the width (stride) is +1 extra pixel
    // hence building image via hand (i.e. unit-test) width would fail if using what gets read as the source of truth.
    // Because of this nature, reading FROM the image, we will have to reduce the width by 1
    let private read (filename: string): RawImageRGB option =
        try
            let image = new Bitmap(filename) // IDisposable
            let realImageWidth = image.Width - 1   // the anomalies of Bitmap reader making width one greater than it really is...

            // NOTE: This will initialize each cells with NULL, so if you get NULL, it's most likely because of programmer error, in
            // which you did not correctly cover each pixels (i.e. you missed out the edge of the image due to -1, etc)
            let arraySize = image.Height * realImageWidth
            let imageArray = Array.zeroCreate arraySize //  pre-allocate array block for performance (way faster than Array.Append! no leak, probably GC kicking in after 1/4 point, gets slower and slower)

            printfn
                "Opened and read file '%A': Width=%A, Height=%A (ArraySize: %A bytes)"
                filename
                realImageWidth
                image.Height
                imageArray.Length

            let retRecord =
                { Width = uint32 realImageWidth
                  Height = uint32 image.Height
                  Data =
                      for y in 0 .. (image.Height - 1) do
                          for x in 0 .. (realImageWidth - 1) do
                              let pixel (*System.Drawing.*) : Color = image.GetPixel(int x, int y)
                              imageArray.[x + (realImageWidth * y)] <- { R = pixel.R; G = pixel.G; B = pixel.B; A = pixel.A }
                      imageArray }
            // verify that last pixel was written (cannot really unit-test actual file reading, so this is here)
            match box retRecord.Data.[arraySize - 1] with
            | null -> failwith "Unable to read entire image of dimensions specified"
            | v -> ignore v
            retRecord |> Some
        with
        | :? ArgumentOutOfRangeException as e ->
            printfn "Argument (either X or Y) for Bitmap.GetPixel(x,y) is out of range (programmer error)"
            printfn "%s" e.Message
            None
        | :? ArgumentException as e ->
            printfn "ArgumentExceptions for '%A'" filename
            printfn "%s" e.Message
            None
        | Failure (msg) ->
            printf "%s" msg
            None
        | e ->
            printfn "Unhandled exception"
            printfn "%s" e.Message
            None

    let readPng filename: RawImageRGB =
        let stopWatch = System.Diagnostics.Stopwatch.StartNew()

        let ret =
            match read filename with
            | Some image -> image
            | None -> failwith (sprintf "Unable to load '%A'" filename)

        stopWatch.Stop()
        printfn "%f mSec" stopWatch.Elapsed.TotalMilliseconds
        ret

    let toRawLibPixelImage (rawImage: RawImageRGB): RawLibPixelImage =
        let stopWatch = System.Diagnostics.Stopwatch.StartNew()
        printfn
            "Processing RawImageRGB: Width=%A, Height=%A, Size=%A bytes"
            rawImage.Width
            rawImage.Height
            rawImage.Data.Length

        let pixelArray = Array.zeroCreate rawImage.Data.Length // just copying the entire image, so dimension must match!

        let retBitMapImage =
            { Width = rawImage.Width
              Height = rawImage.Height
              Pixels =
                  let arraySize = uint32 rawImage.Data.Length - 1u
                  for pxy in 0u .. arraySize do
                      pixelArray.[int pxy] <- makePixel rawImage.Data.[int pxy]
                  pixelArray }

        stopWatch.Stop()
        printfn "%f mSec" stopWatch.Elapsed.TotalMilliseconds

        //if rawImage.Data.Length
        //   <> retBitMapImage.Pixels.Length then
        //    failwith "Programmer error, dimensions of the converted image does not match the original!"

        retBitMapImage

    let private toImageRect width height (byteArray: Pixel []): Pixel [] [] =
        let twoDArray = Array.zeroCreate (height) // creating jagged[][] array, but initializing only the rows
        for y in 0 .. (height - 1) do
            let scanLine = Array.zeroCreate (width) // initialize the columns for each rows
            for x in 0 .. (width - 1) do
                scanLine.[x] <- byteArray.[x + (y * width)]
            twoDArray.[y] <- scanLine
        twoDArray

    let private debugDumpCell rowY colX (cell: Cell) =
        printfn "---------------- Row: %A, Col: %A" rowY colX
        for row in 0u .. (cell.Dimension - 1u) do
            for col in 0u .. (cell.Dimension - 1u) do
                printf "%02X|" cell.Block.[int(row)].[int col].Compressed
            printfn ""

    // on a 10x11 at dim=4, the bottom-right corner block's bottom right pixel coordinate
    // will be based on:
    // * first Scanline (Pixel Y coordinate) of bottom most cell: (cellHeight - 1) * dimension => (2 - 1) * 4 = 4
    // * upper left corner Pixel coordinate of bottom right cell: (cellWidth - 1) * dimension => (2 - 1) * 4 = 4, so (4, 4)
    // * bottom right corner coordinate: (4 + (dimension - 1)), (4 + (dimension - 1)) => (7, 7)
    // * array index = (7 * imageWidth) + 7 = (7 * 10) + 7 = 77
    // NOTE: This calculation will NOT check/test for boundaries/ceilings/floor!  Only verification it can do is
    //       test to make sure internally calculated X position is less than stride length
#if DEBUG
    let private calcIndex pX pY stride maxIndex =
        let i = int ((pY * stride) + pX)
        if i >= maxIndex
        then failwith "pX pY passed exceeds maxIndex"
        i
#else
    let private calcIndex pX pY stride = int ((pY * stride)) + pX)
#endif
#if DEBUG
    let private calcArrayIndex cellX cellY dimension stride maxIndex: int [] [] =
#else
    let private calcArrayIndex cellX cellY dimension stride: int [] [] =
#endif
        let pixelY = cellY * dimension
        let pixelX = cellX * dimension
        let maxCellX = stride / dimension
        if cellX >= maxCellX
        then failwith "Invalid cellX passed which is located outside the stride dimension"
        let cellIndices = Array.zeroCreate (int dimension)
        for y in 0u .. (dimension - 1u) do
            let cells = Array.zeroCreate (int dimension)
            for x in 0u .. (dimension - 1u) do
                let index =
#if DEBUG
                    calcIndex (pixelX + x) (pixelY + y) stride maxIndex
#else
                    calcIndex (pixelX + x) (pixelY + y) stride

#endif
                cells.[int x] <- index
            cellIndices.[int y] <- cells
        cellIndices

    //let private calcCellXY pX pY dimension =
    //    let cellX = pX / dimension
    //    let cellY = pY / dimension
    //    (cellX, cellY)
    let private dumpCopyRectArgs pX pY dimension stride (vector: Pixel []) strMsg =
        strMsg
        + (sprintf "\n\tpX=%A, pY=%A; dimension=%A; stride=%A; vector=%A" pX pY dimension stride vector.Length)

    let private copyRectToCell pX pY dimension stride (vector: Pixel []): Cell =
        let imageEnd = uint32 vector.Length
        let blockBottomY = pY + (dimension - 1u)
        let imageBottomY = blockBottomY * stride  // assume maxX is stride
        let blockRightX = pX + (dimension - 1u)
        let cellX = pX / dimension
        let cellY = pY / dimension
        if blockRightX > (stride - 1u) then
            failwith
                (dumpCopyRectArgs
                    pX
                     pY
                     dimension
                     stride
                     vector
                     (sprintf
                         "Invalid positionX(%A), it (with dimension width added) should not exceed right most edge (stride=%A) value!"
                          pX
                          stride))
        if imageBottomY > imageEnd
        then failwith
                 "Invalid positionY, it (with dimension height added) should not exceed beyond the vector dimension"
        if (uint32 (imageBottomY + blockRightX)) > imageEnd
        then failwith "Invalid (X,Y) coordinate, will lead to index outside the buffer pool"

        let indices =
#if DEBUG
            calcArrayIndex cellX cellY dimension stride vector.Length
#else
            calcArrayIndex cellX cellY dimension stride
#endif
#if DEBUG
        for iY in 0u .. (dimension - 1u) do
            for iX in 0u .. (dimension - 1u) do
                let i = indices.[int iY].[int iX]
                if i >= vector.Length then
                    failwith
                        (sprintf
                            "Invalid index=%A (%A, %A) calculated at (%A, %A) with dimension=%A, stride=%A"
                             i
                             iX
                             iY
                             pX
                             pY
                             dimension
                             stride)
#endif

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
                  //let block = Array.zeroCreate (int dimension)
                  //for vY in pY .. blockBottomY do
                  //    let row = Array.zeroCreate (int dimension)
                  //    for vX in pX .. blockRightX do
                  //        let iVector = (vY * (stride - 1u)) + vX

                  //        let pixel =
#if DEBUG
                  //            match box vector.[int iVector] with
                  //            | null -> failwith "Programmer error, invalid coordinates"
                  //            | v -> vector.[int iVector]
#else
                  //            vector.[int iVector]
#endif
                  //        row.[int (vX - pX)] <- pixel
                  //    block.[int (vY - pY)] <- row
                  //block
                  let block = Array.zeroCreate (int dimension)
                  for iRow in 0u .. (dimension - 1u) do
                      let row = Array.zeroCreate (int dimension)
                      for iCol in 0u .. (dimension - 1u) do
                          let iVector = indices.[int(iRow)].[int (iCol)]

                          let pixel =
#if DEBUG
                              match box vector.[int iVector] with
                              | null -> failwith "Programmer error, invalid coordinates"
                              | v -> vector.[int iVector]
#else
                              vector.[int iVector]

#endif
                          row.[int (iCol)] <- pixel
                      block.[int (iRow)] <- row
                  block }

        retCell

    // Cannot assume `stride = (cellWidthCount * dimension)` since cellWidth truncates the right edge, but one can assume
    // that `stride >= (cellWidthCount * dimension)`
    let private makeCellRow (cellDimension: uint32)
                            (cellWidthCount: uint32) // number of cells in a row
                            (cellY: uint32) // cell Y position based on stride and vector; to calculate pixelY -> scanline = cellY * cellDimension
                            (stride: uint32) // for a single dimension vector, need to know where the edge of the image is
                            (vector: Pixel []) // entire image stream, there are no check when you pass cellY outside the image buffer
                            : Cell [] =
        let bottomVectorY =
            ((stride * cellDimension) * cellY)

        if (cellDimension % 2u) = 1u then failwith "Dimension must be even sized"
        if stride < (cellWidthCount * cellDimension)
        then failwith "Cell count for the row cannot exceed the image width"
        if bottomVectorY > uint32 vector.Length
        then failwith "cellY passed exceeds vector resolution"

        let cellRow = Array.zeroCreate (int cellWidthCount)
        let scanline = cellY * cellDimension // upper left Y of the row we're creating

        for cx in 0u .. (cellWidthCount - 1u) do
            let pX = (cx * cellDimension) // upper left X of the current cell we're going to build
            // create a NxN cell block
            let cell =
                copyRectToCell pX scanline cellDimension stride vector

            cellRow.[int cx] <- cell
        cellRow

    let private cellToByteArray (cellBlock: Pixel [] []) =
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
                    makePixel { R = 1uy; G = 1uy; B = 1uy; A = 255uy } |] |]

        let allCleared =
            [| for _ in 0u .. (cell.Dimension - 1u) do
                [| for _ in 0u .. (cell.Dimension - 1u) do
                    makePixel { R = 0uy; G = 0uy; B = 0uy; A = 0uy } |] |]

        let quaded = toQuad cell
        quaded
        |> Array.sumBy (fun blockRow ->
            cellToByteArray blockRow.Block
            |> Array.sumBy (fun b -> if b.Compressed > 0uy then 1uy else 0uy))
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
                              (arrayedImage: Pixel [])
                              : Cell [] =
        if (dimension % 2u) = 1u then failwith "Dimension must be even sized"

        let cells =
            makeCellRow dimension cellWidth cy imageWidth arrayedImage

        cells

    let toBlock dimension (rawImageBytes: RawLibPixelImage): CellImage =
        let stopWatch = System.Diagnostics.Stopwatch.StartNew()
        let cellWidth = rawImageBytes.Width / dimension
        let cellHeight = rawImageBytes.Height / dimension
        if (dimension % 2u) = 1u then failwith "Dimension must be even sized"

        printfn
            "Processing RawImageBytes: Width=%A, Height=%A, Size=%A bytes; CellWidth=%A CellHeight=%A"
            rawImageBytes.Width
            rawImageBytes.Height
            rawImageBytes.Pixels.Length
            cellWidth
            cellHeight
        if cellHeight = 0u
        then failwith "Invalid image height, in order to make a block, it requires at least the dimension height"
        if cellWidth = 0u
        then failwith "Invalid image width, in order to make a block, it requires at least the dimension width"

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

        stopWatch.Stop()
        printfn "%f mSec" stopWatch.Elapsed.TotalMilliseconds
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
        | (_, _) -> failwith (sprintf "Unhandled exception for value (%i, %i)" cX cY)

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

    let dumpByteImage (sb: StringBuilder) (image: RawLibPixelImage) =
        sb.AppendLine
            (sprintf "RawImageBytes Width: %A, Height: %A, Size: %A" image.Width image.Height image.Pixels.Length)
        |> ignore
        for y in 0u .. (image.Height - 1u) do
            let scanLine = y * image.Width
            sb.Append(sprintf "%4i (width: %4i): " scanLine image.Width)
            |> ignore
            for x in 0u .. (image.Width - 1u) do
                let pixel = image.Pixels.[int (x + scanLine)]
                sb.Append(sprintf "%02X " pixel.Compressed)
                |> ignore
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
                        sb.Append(sprintf "{(%02i,%02i) %02X} " cy cx cell.Block.[int(cy)].[int(cx)].Compressed)
                        |> ignore
                    sb.Append(sprintf " | ") |> ignore
                sb.AppendLine(sprintf "") |> ignore
            sb.AppendLine(sprintf "") |> ignore
        sb.AppendLine(sprintf "") |> ignore
