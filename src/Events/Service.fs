namespace ArrangementService.Events

open ArrangementService.Operators
open ArrangementService

open Models
open Queries
open ErrorMessages

module Service =

    let repo = Repo.from models

    let getEvents ctx =
        result {
            let events = repo.read ctx
            return Seq.map models.dbToDomain events
        }

    let getEventsOrganizedBy organizerEmail ctx =
        result {
            let eventsByOrganizer =
                repo.read ctx
                |> queryEventsOrganizedBy organizerEmail 

            return Seq.map models.dbToDomain eventsByOrganizer
        }

    let getEvent id ctx =
        result {
            let! event =
                repo.read ctx
                |> queryEventBy id
                |> withError (eventNotFound id)

            return models.dbToDomain event
        }

    let createEvent writemodel ctx =
        result {
            return! repo.create (fun id -> models.writeToDomain id writemodel) ctx
        }

    let updateEvent id event ctx =
        result {
            let! oldEvent =
                repo.read ctx
                |> queryEventBy id
                |> withError (eventNotFound id)

            return repo.update event oldEvent
        }

    let deleteEvent id ctx =
        result {
            let! event =
                repo.read ctx
                |> queryEventBy id 
                |> withError (eventNotFound id)

            repo.del event
            return eventSuccessfullyDeleted id
        }
