// This is a FAKE 5.0 script, run using
//    dotnet fake build

#r "paket: groupref fake //"

#if !FAKE
#load ".fake/build.fsx/intellisense.fsx"
#r "netstandard"
#endif

open System
open System.Xml.Linq
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.IO
open Fake.Tools

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

// Information about the project to be used at NuGet and in AssemblyInfo files
let project = "FSharp.Formatting"

let summary = "A package of libraries for building great F# documentation, samples and blogs"

let license = "Apache 2.0 License"

let configuration = DotNet.BuildConfiguration.fromEnvironVarOrDefault "configuration" DotNet.BuildConfiguration.Release

// Folder to deposit deploy artifacts
let artifactsDir = __SOURCE_DIRECTORY__ @@ "artifacts"

// Read release notes document
let release = ReleaseNotes.load "RELEASE_NOTES.md"

let projectRepo = "https://github.com/fsprojects/FSharp.Formatting"

// --------------------------------------------------------------------------------------
// Generate assembly info files with the right version & up-to-date information

Target.create "AssemblyInfo" (fun _ ->
    let info = [
        AssemblyInfo.Product project
        AssemblyInfo.Description summary
        AssemblyInfo.Version release.AssemblyVersion
        AssemblyInfo.FileVersion release.AssemblyVersion
        AssemblyInfo.InformationalVersion release.NugetVersion
        AssemblyInfo.Copyright license
    ]

    AssemblyInfoFile.createFSharp "src/Common/AssemblyInfo.fs" info
    AssemblyInfoFile.createCSharp "src/Common/AssemblyInfo.cs" info
    let versionProps =
        XElement(XName.Get "Project",
            XElement(XName.Get "PropertyGroup",
                XElement(XName.Get "Version", release.NugetVersion),
                XElement(XName.Get "PackageReleaseNotes", String.toLines release.Notes)
            )
        )
    versionProps.Save("version.props")
)

// Clean build results
// --------------------------------------------------------------------------------------

Target.create "Clean" (fun _ ->
    !! artifactsDir
    ++ "temp"
    |> Shell.cleanDirs
    // in case the above pattern is empty as it only matches existing stuff
    ["bin"; "temp"; "tests/bin"]
    |> Seq.iter Directory.ensure
)

// Build library
// --------------------------------------------------------------------------------------

let solutionFile = "FSharp.Formatting.sln"

Target.create "Build" (fun _ ->
    solutionFile
    |> DotNet.build (fun opts -> { opts with Configuration = configuration } )
)

Target.create "Tests" (fun _ ->
    solutionFile
    |> DotNet.test (fun opts ->
        { opts with
            Blame = true
            NoBuild = true
            Framework = Some "net5.0"
            Configuration = configuration
            ResultsDirectory = Some "TestResults"
            Logger = Some "trx"
        })
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    DotNet.pack (fun pack ->
        { pack with
            OutputPath = Some artifactsDir
            Configuration = configuration
        }) solutionFile
)

// Generate the documentation by dogfooding the tools pacakge
// --------------------------------------------------------------------------------------

Target.create "GenerateDocs" (fun _ ->
    Shell.cleanDir ".fsdocs"
    Shell.cleanDir ".packages"
    // Τhe tool has been uninstalled when the
    // artifacts folder was removed in the Clean target.
    DotNet.exec id "tool" ("install --no-cache --version " + release.NugetVersion + " --add-source " + artifactsDir + " --tool-path " + artifactsDir + " FSharp.Formatting.CommandTool")  |> ignore
    CreateProcess.fromRawCommand (artifactsDir @@ "fsdocs") ["build"; "--strict"; "--clean"; "--properties"; "Configuration=Release"]
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore
    // DotNet.exec id "fsdocs" "build --strict --clean --properties Configuration=Release" |> ignore
    // DotNet.exec id "tool" "uninstall --local FSharp.Formatting.CommandTool" |> ignore
    Shell.cleanDir ".packages"
)

Target.create "All" ignore

// clean and recreate assembly inform on release
"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "NuGet"
  ==> "Tests"
  ==> "GenerateDocs"
  ==> "All"

Target.runOrDefault "All"
