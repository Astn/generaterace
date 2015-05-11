open Newtonsoft.Json
open System
open FSharp.Control.Reactive
open System.Reactive.Linq
open System.Reactive.Joins
// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

type entrant = 
    { id : int
      mph : float 
      gender: string
      startAt: TimeSpan
      age: int}

type race = 
    { entrant : entrant
      times : IObservable<TimeSpan> }
      
type cleanRead = 
    { bib : int
      checkpoint : int
      gender: string
      age: int
      time : TimeSpan }

let anEighth = 1.0 / 8.0
let aQuarter = 0.25
let anHour = 60.0
let nowDt = System.DateTime.UtcNow
let now = new System.TimeSpan( nowDt.Ticks )

let toEntrant n = 
    let rnd = new Random(n)
    { id = n
      startAt = System.DateTime.Now.TimeOfDay
      mph = float (rnd.Next(2, 10)) + (rnd.NextDouble() * 2.0 - 1.0) 
      age = rnd.Next(10,70)
      gender = match rnd.NextDouble() with
                | d when d > 0.5 -> "male"
                | _ -> "female"}

let toRace checkpoints e = 

    let rnd = new Random(e.id)
    let times2 = 
        Observable.Generate (0, 
            (fun i -> i < checkpoints), 
            (fun i -> i + 1), 
            (fun i -> float (i) * ((anHour / e.mph) + ((rnd.NextDouble() * aQuarter) - anEighth))),
            (fun i -> 
                let j = float (i) * ((anHour / e.mph) + ((rnd.NextDouble() * aQuarter) - anEighth))
                new DateTimeOffset((now + System.TimeSpan.FromMinutes(j)).Ticks, System.TimeSpan.FromSeconds(0.)))
                )
        |> Observable.map TimeSpan.FromMinutes
        |> Observable.map (fun ct -> ct + e.startAt)
    times2

let generate (checkpoints:int) (entrants:int) : IObservable<cleanRead> = 
    Observable.Generate ( 0,
            (fun i -> i < entrants),
            (fun i -> i + 1),
            (fun i -> i ),
            (fun i -> 
                      let rnd = new Random(i) 
                      let rndOff = (float i * 0.25) + (rnd.NextDouble() * 5.) |> TimeSpan.FromSeconds
                      new DateTimeOffset((nowDt + rndOff), System.TimeSpan.FromSeconds(0.))))
    |> Observable.map toEntrant
    |> Observable.map (fun entrant -> {entrant = entrant; times = toRace checkpoints entrant})
    |> Observable.map (fun i -> 
                           i.times 
                           |> Observable.scanInit (0, System.TimeSpan.FromMinutes(0.)) (fun u t -> (fst u + 1,t)) 
                           |> Observable.map (fun (idx,j) -> 
                                  { bib = i.entrant.id
                                    checkpoint = idx
                                    gender = i.entrant.gender
                                    age = i.entrant.age
                                    time = j }))
    |> Observable.flatmap (fun f -> f)

[<EntryPoint>]
let main argv = 
    match argv with
    | args when args.Length = 2 -> generate (Convert.ToInt32(args.[1])) (Convert.ToInt32(args.[0]))
    | args when args.Length = 1 -> generate (Convert.ToInt32(args.[0])) 3
    | _ -> 
        printfn "usage: (# of entrants) (# of checkpoints)"
        printfn "\t #1: (required) positional number of entrants"
        printfn "\t #2: (required) positional number of checkpoints"
        printfn "expected: #entrants #checkpoints"
        Observable.empty<cleanRead>
    |> Observable.map JsonConvert.SerializeObject
    |> Observable.subscribeWithCallbacks (fun f -> printfn "%s" f) (fun e -> printfn "%A" e) (fun () -> printfn "done")
    |> ignore
    Console.ReadLine() |> ignore
    0
