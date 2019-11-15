namespace ArrangementService.Events

open Giraffe
open ArrangementService.Handler
open ArrangementService.Operators

module EventHandlers =

    let getEvents =
        Service.getEvents
        >> Seq.map Models.domainToView
        >> Ok
        |> handle

    let getEventsForEmployee employeeId =
        Service.getEventsForEmployee employeeId
        >> Seq.map Models.domainToView
        >> Ok
        |> handle

    let getEvent id = Service.getEvent id |> handle

    let deleteEvent id =
        Service.deleteEvent id
        >>= commitTransaction
        |> handle

    let updateEvent id =
        getBody<Models.EventWriteModel>
        >> Result.map (Models.writeToDomain id)
        >>= Service.updateEvent
        >>= commitTransaction
        >> Result.map Models.domainToView
        |> handle

    let createEvent =
        getBody<Models.EventWriteModel>
        >>= resultOk Service.createEvent
        >>= commitTransaction
        >> Result.map Models.domainToView
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
