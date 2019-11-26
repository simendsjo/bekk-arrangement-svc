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
