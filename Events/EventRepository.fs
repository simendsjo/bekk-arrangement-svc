namespace kaSkjerSvc.Repositories

open kaSkjerSvc.Database
open kaSkjerSvc.Models
open kaSkjerSvc.Models.EventModels

module EventRepository =
    let getEvents () =
       dbContext.Dbo.Events
       |> Seq.map EventModels.mapDbEventToDomain 
    
    let getEventsForEmployee employeeId =
      getEvents ()
      |> Seq.filter (fun event ->
          event.ResponsibleEmployee = employeeId)
      
    let getEvent id = getEvents () |> Seq.tryFind (fun event -> event.Id = id)
    
    let deleteEvent id =
        query { for e in dbContext.Dbo.Events do
                where (e.Id = id)
                select (Some e)
                exactlyOneOrDefault }
        |> Option.map (fun e ->
            printfn "%A" e.Id
            e.Delete()
            dbContext.SubmitUpdates()) 
     
    let updateEvent (event : EventDomainModel) =
        let foundEventMaybe = query {
                for e in dbContext.Dbo.Events do
                where (e.Id = event.Id)
                select (Some e)
                exactlyOneOrDefault }
        match foundEventMaybe with
        | Some foundEvent ->
            foundEvent.Title <- event.Title
            foundEvent.Description <- event.Description
            foundEvent.Location <- event.Location
            foundEvent.FromDate <- event.FromDate
            foundEvent.ToDate <- event.ToDate
            foundEvent.ResponsibleEmployee <- event.ResponsibleEmployee
            dbContext.SubmitUpdates()
        | None -> ()
                         
    
    
    
    
    (*printfn "%A" (Seq.head events).Title
    
    let row = events.``Create(FromDate, Location, ResponsibleEmployee, Title, ToDate)``(DateTimeOffset.Now, "Månen", 1437, "Test2", DateTimeOffset.Now)
    dbContext.SubmitUpdates()
    
    (events
    |> Seq.rev
    |> Seq.head).Title
    |> printfn "%A"*)

