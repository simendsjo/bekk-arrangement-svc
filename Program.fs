module kaSkjerSvc.App

open System

open kaSkjerSvc.Database

[<EntryPoint>]
let main argv =
    let events = dbContext.Dbo.Events
    printfn "%A" (Seq.head events).Title
    
    let row = events.``Create(FromDate, Location, ResponsibleEmployee, Title, ToDate)``(DateTimeOffset.Now, "Månen", 1437, "Test2", DateTimeOffset.Now)
    dbContext.SubmitUpdates()
    
    (events
    |> Seq.rev
    |> Seq.head).Title
    |> printfn "%A"
    0
