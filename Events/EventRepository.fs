namespace arrangementSvc.Repositories

open Giraffe
open Microsoft.AspNetCore.Http

open arrangementSvc.Database
open arrangementSvc.Models
open arrangementSvc.Models.EventModels

module EventRepository =
    let events (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().Dbo.Events
    let save (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().SubmitUpdates()

    let eventQuery id ctx =
        query {
            for e in events ctx do
                where (e.Id = id)
                select (Some e)
                exactlyOneOrDefault
        }

    let getEvents: HttpContext -> EventDomainModel seq = events >> Seq.map EventModels.mapDbEventToDomain

    let deleteEvent id ctx =
        eventQuery id ctx
        |> Option.map (fun e ->
            e.Delete()
            save ctx)

    let updateEvent (event: EventDomainModel) ctx =
        eventQuery event.Id ctx
        |> Option.map (fun foundEvent ->
            foundEvent.Title <- event.Title
            foundEvent.Description <- event.Description
            foundEvent.Location <- event.Location
            foundEvent.FromDate <- event.FromDate
            foundEvent.ToDate <- event.ToDate
            foundEvent.ResponsibleEmployee <- event.ResponsibleEmployee
            save ctx
            foundEvent)
        |> Option.map EventModels.mapDbEventToDomain


    let createEvent (event: EventWriteModel) (ctx: HttpContext) =
        let newEvent =
            (events ctx)
                .``Create(FromDate, Location, ResponsibleEmployee, Title, ToDate)``
                (event.FromDate, event.Location, event.ResponsibleEmployee, event.Title, event.ToDate)
        newEvent.Description <- event.Description
        save ctx
        newEvent |> EventModels.mapDbEventToDomain
