// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------
open System
open System.IO
open System.Threading
open System.Diagnostics
open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

let execContext = Context.FakeExecutionContext.Create false "build.fsx" [ ]
Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

let rootDir = __SOURCE_DIRECTORY__ </> ".."

// --------------------------------------------------------------------------------------
// project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "OpenTK.GLWpfControl"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A native WPF control for OpenTK 4.X."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = ""

// List of author names (for NuGet package)
let authors = [ "varon"; "NogginBops" ]

// Tags for your project (for NuGet package)
let tags = "WPF OpenTK OpenGL OpenGLES GLES OpenAL C# F# VB .NET Mono Vector Math Game Graphics Sound"

let copyright = "Copyright (c) 2020 Team OpenTK."

// File system information
let solutionFile  = "GLWpfControl.sln"

let binDir = "./bin/"
let buildDir = binDir </> "build"
let nugetDir = binDir </> "nuget"
let testDir = binDir </> "test"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "opentk"
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "GLWpfControl"

// The url for the raw files hosted
let gitRaw = Environment.environVarOrDefault "gitRaw" "https://raw.github.com/opentk"

// --------------------------------------------------------------------------------------
// The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
let release = ReleaseNotes.load "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith "fsproj" -> Fsproj
    | f when f.EndsWith "csproj" -> Csproj
    | f when f.EndsWith "vbproj" -> Vbproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

let activeProjects =
    !! "src/**/*.??proj"

let releaseProjects =
    !! "src/**/*.??proj"
    -- "src/Example/**"

let install =
    lazy
        (if (DotNet.getVersion id).StartsWith "10" then id
         else DotNet.install (fun options -> { options with Channel = DotNet.CliChannel.Version 10 0 }))

// Set general properties without arguments
let inline dotnetSimple arg = DotNet.Options.lift install.Value arg

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes (projectName:string) =
        [
          AssemblyInfo.Title (projectName)
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion
          AssemblyInfo.CLSCompliant true
          AssemblyInfo.Copyright copyright
        ]

    let getProjectDetails projectPath =
        let projectName = Path.GetFileNameWithoutExtension((string)projectPath)
        ( projectPath,
          Path.GetDirectoryName((string)projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    activeProjects
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> AssemblyInfoFile.createFSharp (folderName @@ "AssemblyInfo.fs") attributes
        | Csproj -> AssemblyInfoFile.createCSharp ((folderName @@ "Properties") @@ "AssemblyInfo.cs") attributes
        | Vbproj -> AssemblyInfoFile.createVisualBasic ((folderName @@ "My Project") @@ "AssemblyInfo.vb") attributes
        )
)

// Copies binaries from default VS location to expected bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs
Target.create "CopyBinaries" (fun _ ->
    releaseProjects
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) @@ "bin/Release", "bin" @@ (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> Shell.copyDir toDir fromDir (fun _ -> true))
)

// --------------------------------------------------------------------------------------
// Clean build results

Target.create "Clean" (fun _ ->
    Shell.cleanDirs ["bin"; "temp"]
)

Target.create "Restore" (fun _ -> DotNet.restore dotnetSimple "GLWpfControl.sln" |> ignore)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
     let setOptions a =
        let customParams = sprintf "/p:PackageVersion=%s /p:ProductVersion=%s" release.AssemblyVersion release.NugetVersion
        DotNet.Options.withCustomParams (Some customParams) (dotnetSimple a)

     for proj in activeProjects do
        DotNet.build setOptions proj

    )

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "CreateNuGetPackage" (fun _ ->
    Directory.CreateDirectory nugetDir |> ignore
    let notes = release.Notes |> List.reduce (fun s1 s2 -> s1 + "\n" + s2)

    for proj in releaseProjects do
        Trace.logf "Creating nuget package for Project: %s\n" proj

        let dir = Path.GetDirectoryName proj
        let templatePath = Path.Combine(dir, "paket")
        let oldTemplateContent = File.ReadAllText templatePath
        let newTemplateContent = oldTemplateContent.Insert(oldTemplateContent.Length,  sprintf "\nversion \n\t%s\nauthors \n\t%s\nowners \n\t%s\n"
                release.NugetVersion
                (authors |> List.reduce (fun s a -> s + " " + a))
                (authors |> List.reduce (fun s a -> s + " " + a))).Replace("#VERSION#", release.NugetVersion)
        File.WriteAllText(templatePath+".template", newTemplateContent);

        Trace.logf "Packing into folder: %s\n" (Path.GetFullPath(nugetDir))
        
        let setParams (p:Paket.PaketPackParams) = 
            { p with
                ToolType = ToolType.CreateLocalTool()
                ReleaseNotes = notes
                OutputPath = Path.GetFullPath(nugetDir)
                WorkingDir = dir
                Version = release.NugetVersion
            }
        Paket.pack setParams
    )


Target.create "BuildPackage" ignore

// ---------
// Release Targets
// ---------

open Fake.Api

Target.create "ReleaseOnGitHub" (fun _ ->
    let token =
        match Environment.environVarOrDefault "opentk_github_token" "" with
        | s when not (System.String.IsNullOrWhiteSpace s) -> s
        | _ ->
            failwith
                "please set the github_token environment variable to a github personal access token with repro access."

    let files = !!"bin/*" |> Seq.toList

    let setParams (p:GitHub.CreateReleaseParams) =
        { p with
            Body = String.Join(Environment.NewLine, release.Notes)
            Prerelease = (release.SemVer.PreRelease <> None)
            TargetCommitish = Fake.Tools.Git.Information.getBranchName "."
        }

    GitHub.createClientWithToken token
    |> GitHub.createRelease gitOwner gitName release.NugetVersion setParams
    |> GitHub.publishDraft
    |> Async.RunSynchronously)

Target.create "ReleaseOnNuGet" (fun _ ->
    let apiKey =
        match Environment.environVarOrDefault "opentk_nuget_api_key" "" with
        | s when not (System.String.IsNullOrWhiteSpace s) -> s
        | _ -> failwith "please set the nuget_api_key environment variable to a nuget access token."

    !! (nugetDir </> "*.nupkg")
    |> Seq.iter
        (DotNet.nugetPush (fun opts ->
            { opts with
                PushParams =
                    { opts.PushParams with
                        ApiKey = Some apiKey
                        Source = Some "nuget.org" } })))

Target.create "ReleaseOnAll" ignore

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore

open Fake.Core.TargetOperators

"Clean"
  ==> "Restore"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "All"
  |> ignore

"All"
  ==> "CreateNuGetPackage"
  ==> "ReleaseOnNuGet"
  ==> "ReleaseOnGithub"
  ==> "ReleaseOnAll"
  |> ignore

// ---------
// Startup
// ---------

[<EntryPoint>]
let main args = 
    try
        match args with
        | [| target |] -> Target.runOrDefault target
        | _ -> Target.runOrDefault "All"
        0
    with e ->
        printfn "%A" e
        1