namespace FSharp.Formatting.CommandTool

open CommandLine

open System.IO
open FSharp.Formatting.Common
open FSharp.Formatting.Literate
open FSharp.Formatting.ApiDocs
open FSharp.Formatting.Literate.Evaluation
open FSharp.Formatting.CommandTool.Common

open Dotnet.ProjInfo
open Dotnet.ProjInfo.Workspace

module Crack =

    let msbuildPropBool (s: string) =
        match s.Trim() with
        | "" -> None
        | Dotnet.ProjInfo.Inspect.MSBuild.ConditionEquals "True" -> Some true
        | _ -> Some false

    let runProcess (log: string -> unit) (workingDir: string) (exePath: string) (args: string) =
        let psi = System.Diagnostics.ProcessStartInfo()
        psi.FileName <- exePath
        psi.WorkingDirectory <- workingDir
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.Arguments <- args
        psi.CreateNoWindow <- true
        psi.UseShellExecute <- false

        use p = new System.Diagnostics.Process()
        p.StartInfo <- psi

        p.OutputDataReceived.Add(fun ea -> log (ea.Data))

        p.ErrorDataReceived.Add(fun ea -> log (ea.Data))

        // printfn "running: %s %s" psi.FileName psi.Arguments

        p.Start() |> ignore
        p.BeginOutputReadLine()
        p.BeginErrorReadLine()
        p.WaitForExit()

        let exitCode = p.ExitCode

        exitCode, (workingDir, exePath, args)


    let getTargetFromProjectFile (file : string) =

        let projDir = Path.GetDirectoryName file

        let projectAssetsJsonPath = Path.Combine(projDir, "obj", "project.assets.json")
        if not(File.Exists(projectAssetsJsonPath)) then
            failwithf "project '%s' not restored" file

        let additionalInfo =
            [ "OutputType"
              "IsTestProject"
              "IsPackable"
            ]
        let gp () = Dotnet.ProjInfo.Inspect.getProperties (["TargetPath"] @ additionalInfo)

        let loggedMessages = System.Collections.Concurrent.ConcurrentQueue<string>()
        let runCmd exePath args = runProcess loggedMessages.Enqueue projDir exePath (args |> String.concat " ")
        let msbuildPath = Dotnet.ProjInfo.Inspect.MSBuildExePath.DotnetMsbuild "dotnet"
        let msbuildExec = Dotnet.ProjInfo.Inspect.msbuild msbuildPath runCmd

        let result = file |> Dotnet.ProjInfo.Inspect.getProjectInfos loggedMessages.Enqueue msbuildExec [gp] []

        let msgs = (loggedMessages.ToArray() |> Array.toList)
        match result with
        | Ok [gpResult] ->
            match gpResult with
            | Ok (Inspect.GetResult.Properties props) ->
                let props = props |> Map.ofList
                let targetPath =
                    match props |> Map.tryFind "TargetPath" with
                    | Some t -> t
                    | None -> failwith "error, 'TargetPath' property not found"
                let msbuildPropBool prop =
                    props |> Map.tryFind prop |> Option.bind msbuildPropBool
                let isTestProject = msbuildPropBool "IsTestProject" |> Option.defaultValue false
                let isLibrary = props |> Map.tryFind "outType" |> Option.map (fun s -> s.ToLowerInvariant()) |> ((=) (Some "library"))
                let isPackable = msbuildPropBool "IsPackable" |> Option.defaultValue false
                (targetPath, isTestProject, isPackable, isLibrary)
            | _ -> failwithf "error - %s" (String.concat "\n" msgs)
        | _ -> failwithf "error - %s" (String.concat "\n" msgs)
                
    let getProjectsFromSlnFile (slnPath : string) =
        let msbuildLocator = MSBuildLocator()
        let loaderConfig = Dotnet.ProjInfo.Workspace.LoaderConfig.Default msbuildLocator
        let loader = Dotnet.ProjInfo.Workspace.Loader.Create(loaderConfig)
        loader.LoadSln(slnPath)
        let projects = [ for KeyValue(k,v) in loader.Projects -> v.ProjectFileName ]
        projects

type CoreBuildOptions(watch) =

    let mutable useWaitForKey = false 

    [<Option("input", Required=false, Default="docs", HelpText = "Input directory of documentation content, defaults to 'docs'.")>]
    member val input = "" with get, set

    [<Option("projects", Required=false, HelpText = "Project files to build API docs for outputs, defaults to all packable projects.")>]
    member val projects = Seq.empty<string> with get, set

    [<Option("output", Default= "output", Required = false, HelpText = "Ouput Directory, defaults to 'output' (optional).")>]
    member val output = "" with get, set

    [<Option("generateNotebooks", Default= false, Required = false, HelpText = "Include 'ipynb' notebooks in outputs.")>]
    member val notebooks = false with get, set

    [<Option("eval", Default= true, Required = false, HelpText = "Evaluate F# fragments in scripts.")>]
    member val eval = true with get, set

    [<Option("noLineNumbers", Required = false, HelpText = "Don't add line numbers, default is to add line numbers (optional).")>]
    member val noLineNumbers = false with get, set

    [<Option("parameters", Required = false, HelpText = "Substitution parameters for templates.")>]
    member val parameters = Seq.empty<string> with get, set

    member x.Execute() =
        let mutable res = 0
        use watcher = (if watch then new FileSystemWatcher(x.input) else null )
        let run () =
            try
                printfn "x.projects = %A" x.projects
                let projects =
                    match Seq.toList x.projects with
                    | [] ->
                        match Directory.GetFiles("*.sln") with
                        | [| sln |] -> Crack.getProjectsFromSlnFile sln
                        | _ -> 
                        Seq.toList (Directory.EnumerateFiles(".", "*.fsproj", EnumerationOptions(RecurseSubdirectories=true)))
                    | ps -> ps
                    
                printfn "projects = %A" projects
                let projectOutputs = List.map Crack.getTargetFromProjectFile projects
                printfn "projectOutputs = %A" projectOutputs
                let refs = [ for (tp, _, _, _) in projectOutputs -> tp ]
                let paths = [ for (tp, _, _, _) in projectOutputs -> Path.GetDirectoryName tp ]
                let parameters = evalPairwiseStrings x.parameters
                let templateFile =
                    let t = Path.Combine(x.input, "_template.html")
                    if File.Exists(t) then
                        Some t
                    else
                        printfn "note, expected template file '%s' to exist, proceeding without template" t
                        None

                Literate.ConvertDirectory(
                    x.input,
                    generateAnchors = true,
                    ?template = templateFile,
                    outputDirectory = x.output,
                    format=OutputKind.Html,
                    ?formatAgent = None,
                    ?lineNumbers = Some (not x.noLineNumbers),
                    processRecursive = true,
                    references = true,
                    ?fsiEvaluator = (if x.eval then Some ( FsiEvaluator() :> _) else None),
                    ?parameters = parameters,
                    includeSource = true
                )
                if x.notebooks then
                    Literate.ConvertDirectory(
                        x.input,
                        generateAnchors = true,
                        template = Path.Combine(x.input, "no-template-for-notebooks.html"),
                        outputDirectory = x.output,
                        format=OutputKind.Pynb,
                        ?formatAgent = None,
                        lineNumbers = true,
                        processRecursive = true,
                        references = true,
                        ?fsiEvaluator = (if x.eval then Some ( FsiEvaluator() :> _) else None),
                        ?parameters = parameters,
                        includeSource = true
                    )

                let initialTemplate2 =
                    let t2 = Path.Combine(x.input, "reference", "_template.html")
                    let t1 = Path.Combine(x.input, "_template.html")
                    if File.Exists(t1) then
                        Some t1
                    elif File.Exists(t2) then
                        Some t2
                    else
                        printfn "note, expected template file '%s' or '%s' to exist, proceeding without template" t1 t2
                        None

                ApiDocs.GenerateHtml (
                    dllFiles = refs,
                    outDir = (if x.output = "" then "output/reference" else Path.Combine(x.output, "reference")),
                    ?parameters = parameters,
                    ?template = initialTemplate2,
                    // TODO: grab source repository metadata from project file
                    //?sourceRepo = (evalString x.sourceRepo),
                    //?sourceFolder = (evalString x.sourceFolder),
                    libDirs = paths
                    )

            with
                | _ as ex ->
                    Log.errorf "received exception :\n %A" ex
                    printfn "Error : \n%O" ex
                    res <- -1

        let monitor = obj()
        let mutable queued = true
        if watch then
            watcher.IncludeSubdirectories <- true
            watcher.NotifyFilter <- NotifyFilters.LastWrite
            useWaitForKey <- true
            watcher.Changed.Add (fun _ ->
                if not queued then
                    queued <- true
                    printfn "Detected change in docs, waiting to rebuild..." 
                    lock monitor (fun () ->
                        queued <- false; run()) ) 
            watcher.EnableRaisingEvents <- true
            printfn "Building docs first time..." 

        lock monitor run
        queued <- false

        waitForKey useWaitForKey
        res

[<Verb("build", HelpText = "build the documentation for a solution based on content and defaults")>]
type BuildCommand() =
    inherit CoreBuildOptions(false)

[<Verb("watch", HelpText = "build the documentation for a solution based on content and defaults and watch")>]
type WatchCommand() =
    inherit CoreBuildOptions(true)

