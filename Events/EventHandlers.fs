namespace ArrangementService.Events

open Giraffe

open ArrangementService.Handler
open ArrangementService.Operators
open ArrangementService.Events.Models

module EventHandlers =

    let getEvents =
        Service.getEvents
        >> Seq.map models.domainToView
        >> Ok
        |> handle

    let getEventsForEmployee employeeId =
        Service.getEventsForEmployee employeeId
        >> Seq.map models.domainToView
        >> Ok
        |> handle

    let getEvent id = Service.getEvent id |> handle

    let deleteEvent id =
        Service.deleteEvent id
        >>= commitTransaction
        |> handle

    let updateEvent id =
        getBody<Models.WriteModel>
        >> Result.map (models.writeToDomain id)
        >>= Service.updateEvent id
        >>= commitTransaction
        >> Result.map models.domainToView
        |> handle

    let createEvent =
        getBody<Models.WriteModel>
        >>= resultOk Service.createEvent
        >>= commitTransaction
        >> Result.map models.domainToView
        |> handle

    let EventRoutes: HttpHandler =
        choose
            [ GET >=> choose
                          [ route "/events" >=> getEvents
                            routef "/events/%i" getEvent
                            routef "/events/employee/%i" getEventsForEmployee ]
              DELETE >=> choose [ routef "/events/%i" deleteEvent ]
              PUT >=> choose [ routef "/events/%i" updateEvent ]
              POST >=> choose [ route "/events" >=> createEvent ] ]
