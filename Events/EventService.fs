namespace ArrangementService.Events

open Giraffe

open ArrangementService
open ArrangementService.Operators

open Models

module Service =

    let repo = Repo.from models

    let eventQuery id ctx =
        query {
            for e in models.table ctx do
                where (models.key e = id)
                select (Some e)
                exactlyOne
        }

    let eventNotFound id = sprintf "Kan ikke finne event %d" id |> RequestErrors.NOT_FOUND
    let cantUpdateEvent id = sprintf "Kan ikke oppdatere event %d" id |> RequestErrors.BAD_REQUEST
    let eventSuccessfullyDeleted id = sprintf "Event %d blei sletta" id |> Ok

    let getEvents = repo.read

    let getEventsForEmployee employeeId = getEvents >> Seq.filter (fun event -> event.ResponsibleEmployee = employeeId)

    let getEvent id =
        repo.read
        >> Seq.tryFind (fun event -> event.Id = id)
        >> withError (eventNotFound id)

    let createEvent writemodel = repo.create (fun id -> models.writeToDomain id writemodel)

    let updateEvent id event =
        eventQuery id
        >> withError (eventNotFound id)
        >> Result.map (repo.update event)

    let deleteEvent id =
        eventQuery id
        >> withError (eventNotFound id)
        >> Result.map repo.del
        >> Result.map (fun _ -> eventSuccessfullyDeleted id)
