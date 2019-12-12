namespace ArrangementService

module Operators =

    let (>>=) f g ctx =
        match f ctx with
        | Ok y -> g y ctx
        | Error y -> Error y

    let resultLift f x = f x >> Ok

    let withError error result =
        match result with
        | Some x -> Ok x
        | None -> Error error

    type ResultBuilder() =
        member this.Return(x) = Ok x
        member this.ReturnFrom(x) = x
        member this.YieldFrom(x) = x
        
        member this.Bind(rx, f) =
            match rx with
            | Ok x -> f x
            | Error e -> Error e

    let result = ResultBuilder()