module Task

open System.Threading.Tasks
open FSharp.Control.Tasks.V2

let bind (f: 'x -> 'y Task) (t: 'x Task): 'y Task =
    task {
        let! x = t
        return! f x
    }

let map (f: 'x -> 'y) (t: 'x Task): 'y Task =
    task {
        let! x = t
        return f x
    }

let wrap (x: 'x): 'x Task =
    task {
        return x
    }
