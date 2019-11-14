namespace arrangementSvc.Handlers

open Giraffe
open Microsoft.AspNetCore.Http

open arrangementSvc.Models.EventModels
open arrangementSvc.Services
open arrangementSvc.Models

module EventHandlers =
    // Bind for reader monaden?
    let (>->) f g x = g (f x) x

    let handle f (next: HttpFunc) (ctx: HttpContext) = json (f ctx) next ctx

    let handleWithError errorMessage f (next: HttpFunc) (ctx: HttpContext) =
        f ctx
        |> function
        | Some result -> json result next ctx
        | None -> errorMessage next ctx

    let getBody<'WriteModel> (ctx: HttpContext) = ctx.BindJsonAsync<'WriteModel>().Result

    let eventNotFound id = sprintf "Kan ikke finne event %d" id |> RequestErrors.NOT_FOUND

    let getEvents =
        EventService.getEvents
        >> Seq.map EventModels.mapDomainEventToView
        |> handle

    let getEventsForEmployee employeeId =
        EventService.getEventsForEmployee employeeId
        >> Seq.map EventModels.mapDomainEventToView
        |> handle

    let getEvent id =
        EventService.getEvent id
        >> Option.map EventModels.mapDomainEventToView
        |> handleWithError (eventNotFound id)

    let deleteEvent id =
        EventService.deleteEvent id
        >> Option.map (fun _ -> Successful.OK "Eventet blei sletta!")
        |> handleWithError (eventNotFound id)

    let updateEvent id =
        getBody<EventWriteModel>
        >> mapWriteEventToDomain id
        >-> EventService.updateEvent
        >> Option.map mapDomainEventToView
        |> handleWithError (eventNotFound id)

    let createEvent =
        getBody<EventWriteModel> >-> EventService.createEvent
        >> EventModels.mapDomainEventToView
        |> handle

    let EventRoutes: HttpHandler =
        choose
            [ GET >=> choose
                          [ route "/events" >=> getEvents
                            routef "/events/id=%i" getEvent
                            routef "/events/employeeId=%i" getEventsForEmployee ]
              DELETE >=> choose [ routef "/events/id=%i" deleteEvent ]
              PUT >=> choose [ routef "/events/id=%i" updateEvent ]
              POST >=> choose [ route "/events" >=> createEvent ] ]
