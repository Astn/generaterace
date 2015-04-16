open Newtonsoft.Json
open System
open FSharp.Data
open FSharp.Control.Reactive
open System.Reactive.Concurrency
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.IO

type chipRead = JsonProvider<""" {"bib":5808,"gender":"male","age":23, "checkpoint":1,"time":"00:06:07.9190000"} """>

let streamReaderUnfold (sr:StreamReader) =
        match sr.ReadLine() with
        | str when str = null -> sr.Dispose(); None
        | str when str.Length = 0 -> sr.Dispose(); None
        | str when str.Length = 1 && Char.IsControl(str.[0]) -> sr.Dispose(); None
        | str -> Some(str,sr)

let loadLines (stream:Stream)=
    stream
    |> (fun stdIn -> new StreamReader(stdIn))
    |> Seq.unfold streamReaderUnfold

type outputRecord = {   groupName : string
                        bib : int
                        time : TimeSpan
                        age : int
                    }

let overallReads (reads:IObservable<chipRead.Root>) =
    reads 
        |> Observable.groupBy (fun item -> item.Checkpoint)
        |> Observable.bind (fun group -> group 
                                            |> Observable.map (fun item-> (sprintf "Overall Checkpoint %i" group.Key ,item)))

let genderReads  (reads:IObservable<chipRead.Root>) =
    reads 
        |> Observable.groupBy (fun item -> (item.Checkpoint,item.Gender))
        |> Observable.bind (fun group -> group
                                            |> Observable.map (fun item-> 
                                            let checkpoint, gender = group.Key
                                            (sprintf "%s Checkpoint %i" gender checkpoint ,item)))

let genderAgeReads  (reads:IObservable<chipRead.Root>) =
    reads 
        |> Observable.groupBy (fun item -> (item.Checkpoint, item.Gender, (item.Age+2)/5))
        |> Observable.bind (fun group -> group 
                                            |> Observable.map (fun item-> 
                                            let chk, gender, ageGroup = group.Key
                                            let ageRange = sprintf "%i-%i" (ageGroup * 5 - 2) ((ageGroup + 1) * 5 - 2)
                                            (sprintf "%s %s Checkpoint %i" gender ageRange chk ,item)))


let groupAllReads (reads:IObservable<chipRead.Root>) = 
    reads |> overallReads
    |> Observable.merge (reads |> genderReads)
    |> Observable.merge (reads |> genderAgeReads)

let stream input = 
        input
            |> Seq.map chipRead.Parse
            |> Observable.ofSeqOn TaskPoolScheduler.Default
            |> Observable.publish
            |> Observable.refCount
            |> groupAllReads  

[<EntryPoint>]
let main argv = 

    let input = if argv.Length = 1 then File.OpenRead argv.[0] :> Stream
                else Console.OpenStandardInput() 

    let lines = loadLines input            
    let mre = new ManualResetEvent(false)
    let printer = lines
                    |> stream
                    |> Observable.map (fun (group,item) -> {groupName=group; bib=item.Bib; time= item.Time.TimeOfDay; age= item.Age;}  )
                    |> Observable.subscribeWithCallbacks  (fun item -> printfn "%s" <| JsonConvert.SerializeObject item ) 
                                                          (fun err -> printfn "Error: %A" err)
                                                          (fun () -> mre.Set() |> ignore)
    
    mre.WaitOne() |> ignore
    printer.Dispose()
    0
