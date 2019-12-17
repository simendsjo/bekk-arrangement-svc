namespace ArrangementService.Events

open ArrangementService.Operators
open ArrangementService

open Queries
open ErrorMessages

module Service =

    let models = Models.models
    let repo = Repo.from models

    let getEvents =
        result {
            for events in repo.read do
            return Seq.map models.dbToDomain events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            for events in repo.read do
            let eventsByOrganizer = queryEventsOrganizedBy organizerEmail events
            return Seq.map models.dbToDomain eventsByOrganizer
        }

    let getEvent id =
        result {
            for events in repo.read do

            let! event =
                events
                |> queryEventBy id
                |> withError (eventNotFound id)

            return models.dbToDomain event
        }

    let createEvent writemodel =
        result {
            for newEvent in
                repo.create (fun id -> models.writeToDomain id writemodel) do

            return newEvent
        }

    let updateEvent id event =
        result {
            for events in repo.read do

            let! oldEvent = 
                events
                |> queryEventBy id
                |> withError (eventNotFound id)

            return repo.update event oldEvent
        }

    let deleteEvent id =
        result {
            for events in repo.read do

            let! event =
                events
                |> queryEventBy id 
                |> withError (eventNotFound id)

            repo.del event
            return eventSuccessfullyDeleted id 
        }
