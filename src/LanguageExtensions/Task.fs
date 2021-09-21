namespace ArrangementService

open FSharp.Control.Tasks.V2
open System.Threading.Tasks

module Task =
    let bind (f: 'x -> ('y Task)) (t: 'x Task): 'y Task =
        task {
            let! x = t
            return! f x
        }

    let map (f: 'x -> 'y) (t: 'x Task): 'y Task =
        task {
            let! x = t
            return f x
        }

    let unit (x: 'x): 'x Task =
        task {
            return x
        }
