namespace ArrangementService.Events

open ArrangementService.Operators
open Giraffe

module Service =
    let eventNotFound id = sprintf "Kan ikke finne event %d" id |> RequestErrors.NOT_FOUND
    let cantUpdateEvent id = sprintf "Kan ikke oppdatere event %d" id |> RequestErrors.BAD_REQUEST

    let getEvents = Repo.getEvents

    let getEventsForEmployee employeeId = getEvents >> Seq.filter (fun event -> event.ResponsibleEmployee = employeeId)

    let getEvent id =
        getEvents
        >> Seq.tryFind (fun event -> event.Id = id)
        >> withError (eventNotFound id)

    let deleteEvent id = Repo.deleteEvent id >> withError (eventNotFound id)

    let updateEvent event = Repo.updateEvent event >> withError (cantUpdateEvent event.Id)

    let createEvent = Repo.createEvent
