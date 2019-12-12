namespace ArrangementService

module Operators =

    let withError error result =
        match result with
        | Some x -> Ok x
        | None -> Error error

    type ResultBuilder() =
        member this.Return(x) = x >> Ok
        member this.ReturnFrom(x) = x
        
        member this.Bind(rx, f) =
            fun ctx ->
                match rx ctx with
                | Ok x -> f x ctx
                | Error e -> Error e

    let result = ResultBuilder()

    let ignoreContext x _ = x