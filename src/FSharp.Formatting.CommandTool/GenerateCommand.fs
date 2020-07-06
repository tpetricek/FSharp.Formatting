namespace FSharp.Formatting.Options.ApiDocs

open CommandLine
open CommandLine.Text
open FSharp.Formatting.ApiDocs

open FSharp.Formatting.Common
open FSharp.Formatting.Options
open FSharp.Formatting.Options.Common
open FSharp.Formatting.Razor


/// Exposes metadata formatting functionality. 
[<Verb("generate", HelpText = "generate API reference docs from metadata")>]
type GenerateOptions() =
    // does not work as desired in F#:
    // the HelpOption attribute is not built,
    // but receive a System.MemberAccessException
    //[<HelpOption>]
    /// autogenerated help text
    member x.GetUsageOfOption() =
        let help = new HelpText()
        help.AddDashesToOption <- true
        //help.AddOptions(x)
        "\nfsformatting metadataFormat --generate [options]" +
        "\n------------------------------------------------" +
        help.ToString()


    [<Option("help", Required = false,
        HelpText = "Display this message. All options are case-insensitive.")>]
    member val help = false with get, set

    [<Option("waitForKey", Required = false,
        HelpText = "Wait for key before exit.")>]
    member val waitForKey = false with get, set

    [<Option("dlls", Required = true,
        HelpText = "DLL input file list.")>]
    member val dlls = Seq.empty<string> with get, set

    [<Option("output", Required = true,
        HelpText = "Output Directory.")>]
    member val output = "" with get, set

    [<Option("layoutRoots", Required = true,
        HelpText = "Search directory list for the Razor Engine.")>]
    member val layoutRoots = Seq.empty<string> with get, set

    [<Option("parameters", Required = false,
        HelpText = "Property settings for the Razor Engine (optinal).")>]
    member val parameters = Seq.empty<string> with get, set

    [<Option("namespaceTemplate", Required = false,
        HelpText = "Namespace template file for formatting, defaults to 'namespaces.cshtml' (optional).")>]
    member val namespaceTemplate = "" with get, set

    [<Option("moduleTemplate", Required = false,
        HelpText = "Module template file for formatting, defaults to 'module.cshtml' (optional).")>]
    member val moduleTemplate = "" with get, set

    [<Option("typeTemplate", Required = false,
        HelpText = "Type template file for formatting, defaults to 'type.cshtml' (optional).")>]
    member val typeTemplate = "" with get, set

    [<Option("xmlFile", Required = false,
        HelpText = "Single XML file to use for all DLL files, otherwise using 'file.xml' for each 'file.dll' (optional).")>]
    member val xmlFile = "" with get, set

    [<Option("sourceRepo", Required = false,
        HelpText = "Source repository URL; silently ignored, if source repository folder is not provided (optional).")>]
    member val sourceRepo = "" with get, set

    [<Option("sourceFolder", Required = false,
        HelpText = "Source repository folder; silently ignored, if source repository URL is not provided (optional).")>]
    member val sourceFolder = "" with get, set

    [<Option("libDirs", Required = false,
        HelpText = "Search directory list for library references.")>]
    member val libDirs = Seq.empty<string> with get, set

    member x.Execute() =
        let mutable res = 0
        try
            if x.help then
                printfn "%s" (x.GetUsageOfOption())
            else
                RazorMetadataFormat.Generate (
                    dllFiles = (x.dlls |> List.ofSeq),
                    outDir = x.output,
                    layoutRoots = (x.layoutRoots |> List.ofSeq),
                    ?parameters = (evalPairwiseStrings x.parameters),
                    ?namespaceTemplate = (evalString x.namespaceTemplate),
                    ?moduleTemplate = (evalString x.moduleTemplate),
                    ?typeTemplate = (evalString x.typeTemplate),
                    ?xmlFile = (evalString x.xmlFile),
                    ?sourceRepo = (evalString x.sourceRepo),
                    ?sourceFolder = (evalString x.sourceFolder),
                    ?libDirs = (evalStrings x.libDirs)
                    )
        with ex ->
            Log.errorf "received exception in RazorMetadataFormat.Generate:\n %A" ex
            printfn "Error on RazorMetadataFormat.Generate: \n%O" ex
            res <- -1
        waitForKey x.waitForKey
        res

