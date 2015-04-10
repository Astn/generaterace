open Newtonsoft.Json
open System
open FSharp.Data
open FSharp.Control.Reactive
open System.Reactive.Concurrency
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading

type chipRead = JsonProvider<""" {"bib":5808,"gender":"male","age":23, "checkpoint":1,"time":"00:06:07.9190000"} """>

let loadFileLines (path:string) =
     new IO.StreamReader(path)
     |> Seq.unfold (fun sr ->
        match sr.ReadLine() with
        | null -> sr.Dispose(); None
        | str -> Some(str,sr))

let groupReads (reads:IObservable<chipRead.Root>) = 
    let overall = reads 
                    |> Observable.groupBy (fun item -> item.Checkpoint)
                    |> Observable.bind (fun group -> group 
                                                        |> Observable.take 8
                                                        |> Observable.map (fun item-> (sprintf "Overall Checkpoint %i" group.Key ,item)))
    let overallGender = reads 
                            |> Observable.groupBy (fun item -> (item.Checkpoint,item.Gender))
                            |> Observable.bind (fun group -> group
                                                                |> Observable.take 8
                                                                |> Observable.map (fun item-> (sprintf "%s Checkpoint %i" (snd group.Key) (fst group.Key) ,item)))
    let genderAge = reads 
                        |> Observable.groupBy (fun item -> (item.Checkpoint, item.Gender, (item.Age+2)/5))
                        |> Observable.bind (fun group -> group 
                                                            |> Observable.take 8
                                                            |> Observable.map (fun item-> 
                                                            let chk, gender, ageGroup = group.Key
                                                            let ageRange = sprintf "%i-%i" (ageGroup * 5 - 2) ((ageGroup + 1) * 5 - 2)
                                                            (sprintf "%s %s Checkpoint %i" gender ageRange chk ,item)))
    overall
        |> Observable.merge overallGender
        |> Observable.merge genderAge

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
                    |> Observable.publish
                    |> Observable.refCount

        let allRanks = groupReads reads

        let printer = allRanks
                        |> Observable.subscribe (fun (group,item) ->
                            printfn "%s bib: %i at %A age: %i" group item.Bib item.Time.TimeOfDay item.Age)   
    
        Console.ReadLine() |> ignore
        printer.Dispose()
        0

[<EntryPoint>]
let main argv = 
    let file = 
        match argv with
        | args when args.Length = 1 -> args.[0]
        | _ -> ""
    
    stream file

    
