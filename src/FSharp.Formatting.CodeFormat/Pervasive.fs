[<AutoOpen>]
module internal FSharp.Formatting.CodeFormat.Pervasive

open System
open System.Diagnostics
open System.Runtime.CompilerServices
open FSharp.Compiler.Syntax
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text

[<assembly: InternalsVisibleTo("FSharp.CodeFormat.Tests")>]
do()

[<Sealed>]
type AsyncMaybeBuilder () =

    [<DebuggerStepThrough>]
    member __.Return value : Async<'T option> = Some value |> async.Return

    [<DebuggerStepThrough>]
    member __.ReturnFrom value : Async<'T option> = value

    [<DebuggerStepThrough>]
    member __.ReturnFrom (value: 'T option) : Async<'T option> = async.Return value

    [<DebuggerStepThrough>]
    member __.Zero () : Async<unit option> = Some () |> async.Return

    [<DebuggerStepThrough>]
    member __.Delay (f : unit -> Async<'T option>) : Async<'T option> = async.Delay f

    [<DebuggerStepThrough>]
    member __.Combine (r1, r2 : Async<'T Option>) : Async<'T option> = async {
        let! r1' = r1
        match r1' with
        | None -> return None
        | Some () -> return! r2
    }

    [<DebuggerStepThrough>]
    member __.Bind (value: Async<'T option>, f : 'T -> Async<'U option>) : Async<'U option> = async {
        let! value' = value
        match value' with
        | None -> return None
        | Some result -> return! f result
    }

    [<DebuggerStepThrough>]
    member __.Bind (value: System.Threading.Tasks.Task<'T>, f : 'T -> Async<'U option>) : Async<'U option> = async {
        let! value' = Async.AwaitTask value
        return! f value'
    }

    [<DebuggerStepThrough>]
    member __.Bind (value: 'T option, f : 'T -> Async<'U option>) : Async<'U option> = async {
        match value with
        | None -> return None
        | Some result -> return! f result
    }

    [<DebuggerStepThrough>]
    member __.Using (resource : ('T :> IDisposable), body : _ -> Async<_ option>) : Async<_ option> =
        try body resource
        finally if not (isNull resource) then resource.Dispose ()

    [<DebuggerStepThrough>]
    member x.While (guard, body : Async<_ option>) : Async<_ option> =
        if guard () then
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    [<DebuggerStepThrough>]
    member x.For (sequence : seq<_>, body : 'T -> Async<unit option>) : Async<_ option> =
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (enum.MoveNext, x.Delay (fun () -> body enum.Current)))

    [<DebuggerStepThrough>]
    member inline __.TryWith (computation : Async<'T option>, catchHandler : exn -> Async<'T option>) : Async<'T option> =
            async.TryWith (computation, catchHandler)

    [<DebuggerStepThrough>]
    member inline __.TryFinally (computation : Async<'T option>, compensation : unit -> unit) : Async<'T option> =
            async.TryFinally (computation, compensation)

let asyncMaybe = AsyncMaybeBuilder()

let inline liftAsync (computation : Async<'T>) : Async<'T option> = async {
    let! a = computation
    return Some a
}


[<RequireQualifiedAccess>]
module Async =

    let map (f: 'T -> 'U) (a: Async<'T>) : Async<'U> = async {
        let! a = a
        return f a
    }

    /// Creates an asynchronous workflow that runs the asynchronous workflow given as an argument at most once.
    /// When the returned workflow is started for the second time, it reuses the result of the previous execution.
    let cache (input : Async<'T>) =
        let agent = MailboxProcessor<AsyncReplyChannel<_>>.Start <| fun agent ->
            async {
                let! replyCh = agent.Receive ()
                let! res = input
                replyCh.Reply res
                while true do
                    let! replyCh = agent.Receive ()
                    replyCh.Reply res
            }
        async { return! agent.PostAndAsyncReply id }

type CheckResults =
    | Ready of (FSharpParseFileResults * FSharpCheckFileResults) option
    | StillRunning of Async<(FSharpParseFileResults * FSharpCheckFileResults) option>

type FSharpChecker with

    member this.ParseAndCheckDocument(filePath: string, sourceText: string, options: FSharpProjectOptions, allowStaleResults: bool) : Async<(FSharpParseFileResults * ParsedInput * FSharpCheckFileResults) option> =
            let parseAndCheckFile = async {
                let! parseResults, checkFileAnswer = this.ParseAndCheckFileInProject(filePath, 0, SourceText.ofString sourceText, options)
                return
                    match checkFileAnswer with
                    | FSharpCheckFileAnswer.Aborted -> None
                    | FSharpCheckFileAnswer.Succeeded checkFileResults -> Some (parseResults, checkFileResults)
            }

            let tryGetFreshResultsWithTimeout () : Async<CheckResults> = async {
                try let! worker = Async.StartChild (parseAndCheckFile, 2000)
                    let! result = worker
                    return Ready result
                with :? TimeoutException -> return StillRunning parseAndCheckFile
            }

            let bindParsedInput (results: (FSharpParseFileResults * FSharpCheckFileResults) option) =
                match results with
                | Some (parseResults, checkResults) -> Some (parseResults, parseResults.ParseTree, checkResults)
                | None -> None

            if allowStaleResults then
                async {
                    let! freshResults = tryGetFreshResultsWithTimeout()

                    let! results =
                        match freshResults with
                        | Ready x -> async.Return x
                        | StillRunning worker ->
                            async {
                                match allowStaleResults, this.TryGetRecentCheckResultsForFile(filePath, options) with
                                | true, Some (parseResults, checkFileResults, _) ->
                                    return Some (parseResults, checkFileResults)
                                | _ ->
                                    return! worker
                            }
                    return bindParsedInput results
                }
            else parseAndCheckFile |> Async.map bindParsedInput

