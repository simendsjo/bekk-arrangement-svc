namespace ArrangementService

open System

module TimeStamp =

    // Milliseconds since epoch
    type TimeStamp =
        | TimeStamp of int64
        member this.Unwrap =
            match this with
            | TimeStamp t -> t

    let now(): TimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds() |> TimeStamp
