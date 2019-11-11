namespace kaSkjerSvc.Repositories

open kaSkjerSvc.Database
open kaSkjerSvc.Models
open kaSkjerSvc.Models.EventModels

module EventRepository =
    let getEvents (dbContext : ArrangementDbContext) =
       dbContext.Dbo.Events
       |> Seq.map EventModels.mapDbEventToDomain 
    
    let getEventsForEmployee employeeId (dbContext : ArrangementDbContext) =
      getEvents dbContext
      |> Seq.filter (fun event ->
          event.ResponsibleEmployee = employeeId)
      
    let getEvent id (dbContext : ArrangementDbContext) =
        getEvents dbContext |> Seq.tryFind (fun event -> event.Id = id)
    
    let deleteEvent id (dbContext : ArrangementDbContext) =
        query { for e in dbContext.Dbo.Events do
                where (e.Id = id)
                select (Some e)
                exactlyOneOrDefault }
        |> Option.map (fun e ->
            e.Delete()
            dbContext.SubmitUpdates()) 
     
    let updateEvent (event : EventDomainModel) (dbContext : ArrangementDbContext) =
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
            foundEvent |> EventModels.mapDbEventToDomain |> Some
        | None -> None
                         
    
    let createEvent (event : EventWriteModel) (dbContext : ArrangementDbContext) =
        let newEvent = dbContext.Dbo.Events.``Create(FromDate, Location, ResponsibleEmployee, Title, ToDate)``
                           (event.FromDate,
                           event.Location,
                           event.ResponsibleEmployee,
                           event.Title,
                           event.ToDate)
        newEvent.Description <- event.Description
        dbContext.SubmitUpdates()
        newEvent |> EventModels.mapDbEventToDomain
