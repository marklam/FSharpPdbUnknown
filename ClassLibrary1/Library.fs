namespace ClassLibrary1

open FSharp.UMX

type SizeBase<'T when 'T : unmanaged> (width : int, height : int) =
    member __.Width  with get() = width
    member __.Height with get() = height

type Size16 (width : int, height : int) =
    inherit SizeBase<uint16>(width, height)

type Size8 (width : int, height : int) =
    inherit SizeBase<byte>(width, height)

type SizeFP (width : int, height : int) =
    inherit SizeBase<double>(width, height)

type ISize<'T> =
    abstract Width  : int with get
    abstract Height : int with get

[<Struct>]
type SizeBaseAdapter<'pix when 'pix : unmanaged>(size : SizeBase<'pix>) =
    interface ISize<'pix> with
        member __.Width  with get() = size.Width
        member __.Height with get() = size.Height

type ImageBase<
                [<Measure>]'xy,
                'pix,
                'pixSpace,
                'image,
                'createdImage
                    when
                        'pix          :  unmanaged      and
                        'image        :> ISize<'pix> and
                        'createdImage :> ISize<'pix>
                >
    (
        width  : int<'xy>,
        height : int<'xy>,
        makeImage    : int * int     -> 'createdImage,
        image : 'createdImage
    ) =

    new (width, height, makeImage : int * int -> 'createdImage) =
        let dest = makeImage(int width, int height)
        ImageBase(width, height, makeImage, dest)

type Image16<[<Measure>]'xy> = ImageBase<'xy, uint16, uint16, SizeBaseAdapter<uint16>, SizeBaseAdapter<uint16>>

module SizeAdapter =
    let inline rw img = SizeBaseAdapter(img)

type SizeAdapter =
    static member inline Create(_ : SizeAdapter, _ : uint16, width, height) =
        Size16(width, height)
        |> SizeAdapter.rw

    static member inline Create(_ : SizeAdapter, _ : byte, width, height) =
        Size8(width, height)
        |> SizeAdapter.rw

    static member inline Create(_ : SizeAdapter, _ : float, width, height) =
        SizeFP(width, height)
        |> SizeAdapter.rw

    static member inline create (width, height) : SizeBaseAdapter<'pix> =
        let inline call_2 (a : ^a, b: ^b, c : int, d : int) = ((^a or ^b) : (static member Create : _*_*int*int -> _) (a, b, c, d))
        let inline call (a : 'a, b : 'b) = fun (width, height) -> call_2 (a, b, width, height)
        call (Unchecked.defaultof<SizeAdapter>, Unchecked.defaultof<'pix>) (width, height)

[<RequireQualifiedAccess>]
module ImageData =
    let read skip =
        if skip
        then Choice1Of2 (Size8(1,1))
        else Choice2Of2 (Size16(1,1))

    let inline partial skipSections =
        match read skipSections with
        | Choice1Of2 img ->
            Image16(UMX.tag<'xy> img.Width, UMX.tag<'xy> img.Height, SizeAdapter.create)
        | Choice2Of2 img ->
            Image16(UMX.tag<'xy> img.Width, UMX.tag<'xy> img.Height, SizeAdapter.create)

    let inline load () =
        partial true