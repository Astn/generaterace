open Newtonsoft.Json
open System

// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

type entrant = 
    { id : int
      mph : float 
      gender: string
      age: int}

type race = 
    { entrant : entrant
      times : seq<TimeSpan> }
      
type cleanRead = 
    { bib : int
      checkpoint : int
      gender: string
      age: int
      time : TimeSpan }

let anEighth = 1.0 / 8.0
let aQuarter = 0.25
let anHour = 60.0

let toEntrant n = 
    let rnd = new Random(n)
    { id = n
      mph = float (rnd.Next(2, 10)) + (rnd.NextDouble() * 2.0 - 1.0) 
      age = rnd.Next(10,70)
      gender = match rnd.NextDouble() with
                | d when d > 0.5 -> "male"
                | _ -> "female"}

let toRace checkpoints e = 
    let rnd = new Random(e.id)
    
    let times = 
        seq { 
            for i in 0..checkpoints do
                yield float (i) * ((anHour / e.mph) + ((rnd.NextDouble() * aQuarter) - anEighth))
        }
        |> Seq.map TimeSpan.FromMinutes
    { entrant = e
      times = times }

let generate checkpoints entrants = 
    let toRaceWithCheckpoints = toRace checkpoints
    { 0..entrants }
    |> Seq.map toEntrant
    |> Seq.map toRaceWithCheckpoints
    |> Seq.map (fun i -> 
           i.times |> Seq.mapi (fun idx j -> 
                          { bib = i.entrant.id
                            checkpoint = idx
                            gender = i.entrant.gender
                            age = i.entrant.age
                            time = j }))
    |> Seq.collect (fun f -> f)

let generateSimple = generate 3

[<EntryPoint>]
let main argv = 
    match argv with
    | args when args.Length = 2 -> generate (Convert.ToInt32(args.[1])) (Convert.ToInt32(args.[0]))
    | args when args.Length = 1 -> generateSimple (Convert.ToInt32(args.[0]))
    | _ -> 
        printfn "usage: (# of entrants) (# of checkpoints)"
        printfn "\t #1: (required) positional number of entrants"
        printfn "\t #2: (required) positional number of checkpoints"
        printfn "expected: #entrants #checkpoints"
        Seq.empty<cleanRead>
    |> Seq.sortBy (fun f -> f.time)
    |> Seq.map JsonConvert.SerializeObject
    |> Seq.iter (fun f -> printfn "%s" f)
    |> ignore
    0
