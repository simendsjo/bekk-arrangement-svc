namespace ArrangementService.Events

open ArrangementService.Operators
open ArrangementService

open Models
open Queries
open ErrorMessages

module Service =

    let repo = Repo.from models

    let getEvents = repo.read

    let getEventsForEmployee employeeId =
        repo.query
        >> queryEventsForEmployee employeeId
        >> Seq.map models.dbToDomain

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
