// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
type entrant = {id:int; mph:float}
type race = {entrant:entrant; times:seq<System.TimeSpan>}
type cleanRead = {bib:int; checkpoint:int; time:System.TimeSpan}

let anEighth = 1.0 / 8.0
let aQuarter = 0.25
let anHour = 60.0

let toEntrant n =
    let rnd = new System.Random(n)
    { id = n; mph = float(rnd.Next(2,10)) + (rnd.NextDouble() * 2.0 - 1.0)}

let toRace checkpoints e =
    let rnd = new System.Random(e.id)
    let times = seq{for i in 0..checkpoints do  
                        yield float(i) * ((anHour / e.mph) + ((rnd.NextDouble() * aQuarter) - anEighth))
                   }
                |> Seq.map System.TimeSpan.FromMinutes
    {entrant=e; times= times}

let generate checkpoints entrants =
    let toRaceWithCheckpoints = toRace checkpoints
    [0 .. entrants]
    |> Seq.map toEntrant
    |> Seq.map toRaceWithCheckpoints

let generateSimple = generate 3

[<EntryPoint>]
let main argv = 
    match argv with
    | p when p.Length = 2 -> generate (System.Convert.ToInt32(p.[1])) (System.Convert.ToInt32(p.[0]))
    | p when p.Length = 1 -> generateSimple (System.Convert.ToInt32(p.[0]))
    | _ -> printfn "expected: #entrants #checkpoints"  
           Seq.empty<race>
    |> Seq.map (fun i -> i.times 
                            |> Seq.mapi (fun idx j -> {bib=i.entrant.id; checkpoint=idx; time=j;}) 
                )
    |> Seq.collect (fun f -> f)
    |> Seq.sortBy (fun f -> f.time)
    |> Seq.map Newtonsoft.Json.JsonConvert.SerializeObject
    |> Seq.iter (fun f -> printfn "%s" f)
    |> ignore
    0