namespace ArrangementService

open System

module TimeStamp =

    // Milliseconds since epoch
    type TimeStamp = TimeStamp of int64

    let now (): TimeStamp =
      DateTimeOffset.Now.ToUnixTimeSeconds() |> TimeStamp