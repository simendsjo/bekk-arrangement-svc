namespace ArrangementService.Events

open ArrangementService.Operators
open ArrangementService

open Models
open Queries
open ErrorMessages

module Service =

    let repo = Repo.from models

    let getEvents =
        result {
            let! events = repo.read
            return Seq.map models.dbToDomain events |> ignoreContext
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! events = repo.read 
            let eventsByOrganizer = queryEventsOrganizedBy organizerEmail events
            return Seq.map models.dbToDomain eventsByOrganizer |> ignoreContext
        }

    let getEvent id =
        result {
            let! events = repo.read
            let! event =
                events
                |> queryEventBy id
                |> withError (eventNotFound id)
                |> ignoreContext

            return models.dbToDomain event |> ignoreContext
        }

    let createEvent writemodel =
        result {
            return! repo.create (fun id -> models.writeToDomain id writemodel)
        }

    let updateEvent id event =
        result {
            let! events = repo.read 
            let! oldEvent = 
                events
                |> queryEventBy id
                |> withError (eventNotFound id)
                |> ignoreContext

            return repo.update event oldEvent |> ignoreContext
        }

    let deleteEvent id =
        result {
            let! events = repo.read
            let! event =
                events
                |> queryEventBy id 
                |> withError (eventNotFound id)
                |> ignoreContext

            repo.del event
            return eventSuccessfullyDeleted id |> ignoreContext
        }
