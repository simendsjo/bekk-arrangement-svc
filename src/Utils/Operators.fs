namespace ArrangementService

module Operators =

    let withError error result =
        match result with
        | Some x -> Ok x
        | None -> Error error

    type ResultBuilder() =
        member this.Return(x) = fun _ -> Ok x
        member this.ReturnFrom(x) = x
        member this.Yield(f) = f >> Ok
        member this.Delay(f) = f()

        member this.Combine(lhs, rhs) =
            fun ctx ->
                match lhs ctx, rhs ctx with
                | Ok _, rhs -> rhs
                | Error e, _ -> Error e

        member this.Bind(rx, f) =
            fun ctx ->
                match rx with
                | Ok x -> f x ctx
                | Error e -> Error e
        
        member this.For(rx, f) =
            fun ctx ->
                this.Bind(rx ctx, f) ctx

    let result = ResultBuilder()
