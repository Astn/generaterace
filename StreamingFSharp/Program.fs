open Newtonsoft.Json
open System
open FSharp.Data
open FSharp.Control.Reactive
open System.Reactive.Concurrency

type chipRead = JsonProvider<""" {"bib":5808,"checkpoint":1,"time":"00:06:07.9190000"} """>

let loadFileLines (path:string) =
     new IO.StreamReader(path)
     |> Seq.unfold (fun sr ->
        match sr.ReadLine() with
        | null -> sr.Dispose(); None
        | str -> Some(str,sr))

let stream fileLocation = 
    if not <| System.IO.File.Exists fileLocation then
        printfn "the file [%A] does not exist" fileLocation
        1
    else
        printfn "loading %A" fileLocation

        let reads = loadFileLines fileLocation
                    |> Seq.map chipRead.Parse
                    |> Observable.ofSeqOn TaskPoolScheduler.Default
                    |> Observable.delay (TimeSpan.FromMilliseconds(100.0))

        let groups = reads 
                        |> Observable.groupBy (fun item -> item.Checkpoint)
                        |> Observable.subscribe (fun group -> 
                            group 
                            |> Observable.take 8
                            |> Observable.subscribe (fun item ->
                                match item.Checkpoint with
                                | i when i%2=0 ->Console.ForegroundColor <- ConsoleColor.Yellow
                                | _ ->Console.ResetColor()
                                printfn "chk: %i bib: %i at %A" item.Checkpoint item.Bib item.Time.TimeOfDay
                                )
                            |> ignore
                        )       
        Console.ReadLine() |> ignore
        groups.Dispose()
        0

[<EntryPoint>]
let main argv = 
    let file = 
        match argv with
        | args when args.Length = 1 -> args.[0]
        | _ -> ""
    
    stream file

    
