namespace ArrangementService.Events

open Giraffe
open Microsoft.AspNetCore.Http
open ArrangementService.Database

module Repo =
    let events (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().Dbo.Events

    let eventQuery id ctx =
        query {
            for e in events ctx do
                where (e.Id = id)
                select (Some e)
                exactlyOneOrDefault
        }

    let getEvents: HttpContext -> Models.EventDomainModel seq = events >> Seq.map Models.mapDbEventToDomain

    let deleteEvent id ctx = eventQuery id ctx |> Option.map (fun e -> e.Delete())

    let updateEvent (event: Models.EventDomainModel) ctx =
        eventQuery event.Id ctx
        |> Option.map (fun foundEvent ->
            foundEvent.Title <- event.Title
            foundEvent.Description <- event.Description
            foundEvent.Location <- event.Location
            foundEvent.FromDate <- event.FromDate
            foundEvent.ToDate <- event.ToDate
            foundEvent.ResponsibleEmployee <- event.ResponsibleEmployee
            foundEvent)
        |> Option.map Models.mapDbEventToDomain


    let createEvent (event: Models.EventWriteModel) (ctx: HttpContext) =
        let newEvent =
            (events ctx)
                .``Create(FromDate, Location, ResponsibleEmployee, Title, ToDate)``
                (event.FromDate, event.Location, event.ResponsibleEmployee, event.Title, event.ToDate)
        newEvent.Description <- event.Description
        newEvent |> Models.mapDbEventToDomain
