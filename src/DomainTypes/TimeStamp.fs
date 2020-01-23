namespace ArrangementService

open System

// Milliseconds since epoch
type TimeStamp =
    | TimeStamp of int64
    member this.Unwrap =
        match this with
        | TimeStamp t -> t

module TimeStamp =

    let now(): TimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds() |> TimeStamp
