open System.IO
open System.Reflection.Metadata
open System.Reflection.PortableExecutable

[<EntryPoint>]
let main _argv =
    // Reference the code that results in the 'unknown' record
    ClassLibrary1.ImageData.load ()
    |> ignore

    // Dump our Document records from the pdb
    let modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location
    use moduleStream = File.OpenRead(modulePath)
    use peReader = new PEReader(moduleStream)
    for entry in peReader.ReadDebugDirectory() do
        if entry.Type = DebugDirectoryEntryType.CodeView then
            let codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry)
            if File.Exists codeViewData.Path then
                use pdbStream = File.OpenRead(codeViewData.Path)
                let metadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream)
                let metadataReader = metadataReaderProvider.GetMetadataReader()
                for docHandle in metadataReader.Documents do
                    let document = metadataReader.GetDocument(docHandle)
                    if not (document.Name.IsNil) then
                        let documentName = metadataReader.GetString(document.Name)
                        printfn "%s" documentName

    0

