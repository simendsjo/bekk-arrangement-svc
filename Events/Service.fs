namespace ArrangementService.Events

open Giraffe
open System.Linq

open ArrangementService.Operators

open Models

module Service =

    let repo = Repo.from models

    let queryEventBy id (events: IQueryable<DbModel>) =
        query {
            for event in events do
                where (event.Id = id)
                select (Some event)
                exactlyOne
        }

    let eventNotFound id = sprintf "Kan ikke finne event %d" id |> RequestErrors.NOT_FOUND
    let cantUpdateEvent id = sprintf "Kan ikke oppdatere event %d" id |> RequestErrors.BAD_REQUEST
    let eventSuccessfullyDeleted id = sprintf "Event %d blei sletta" id |> Ok

    let getEvents = repo.read

    let getEventsForEmployee employeeId = repo.read >> Seq.filter (fun event -> event.ResponsibleEmployee = employeeId)

    let getEvent id =
        repo.query
        >> queryEventBy id
        >> withError (eventNotFound id)

    let createEvent writemodel = repo.create (fun id -> models.writeToDomain id writemodel)

    let updateEvent id event =
        repo.query
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map (repo.update event)

    let deleteEvent id =
        repo.query
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map repo.del
        >> Result.map (fun _ -> eventSuccessfullyDeleted id)
