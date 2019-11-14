namespace ArrangementService.Events

open Giraffe
open ArrangementService.Handler

module EventHandlers =

    let eventNotFound id = sprintf "Kan ikke finne event %d" id |> RequestErrors.NOT_FOUND

    let getEvents =
        Service.getEvents
        >> Seq.map Models.domainToView
        |> handle

    let getEventsForEmployee employeeId =
        Service.getEventsForEmployee employeeId
        >> Seq.map Models.domainToView
        |> handle

    let getEvent id =
        Service.getEvent id
        >> Option.map Models.domainToView
        |> handleWithError (eventNotFound id)

    let deleteEvent id =
        Service.deleteEvent id
        >> Option.map (fun _ -> Successful.OK "Eventet blei sletta!")
        |> handleWithError (eventNotFound id)

    let updateEvent id =
        getBody<Models.EventWriteModel>
        >> Models.writeToDomain id
        >-> Service.updateEvent
        >> Option.map Models.domainToView
        |> handleWithError (eventNotFound id)

    let createEvent =
        getBody<Models.EventWriteModel> >-> Service.createEvent
        >> Models.domainToView
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
