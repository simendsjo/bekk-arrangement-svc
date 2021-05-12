namespace ArrangementService.Event

open ArrangementService
open ArrangementService.Email
open Models
open ResultComputationExpression
open Queries
open UserMessages
open ArrangementService.DomainModels
open Config

module Service =

    let repo = Repo.from models

    let getEvents =
        result {
            let! events = repo.read
            return Seq.map models.dbToDomain events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! events = repo.read

            let eventsByOrganizer =
                queryEventsOrganizedBy organizerEmail events
            return Seq.map models.dbToDomain eventsByOrganizer
        }

    let getEvent id =
        result {
            let! events = repo.read

            let! event =
                events
                |> queryEventBy id
                |> ignoreContext
                
            return models.dbToDomain event
        }

    let private createdEventMessage createEditUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du har nå opprettet %s." event.Title.Unwrap
          sprintf "Her er en unik lenke for å endre arrangementet: %s."
              (createEditUrl event)
          "Ikke del denne med andre🕵️" ]
        |> String.concat "\n"

    let private createEmail createEditUrl (event: Event) =
        let message = createdEventMessage createEditUrl event
        { Subject = sprintf "Du opprettet %s" event.Title.Unwrap
          Message = message
          To = event.OrganizerEmail
          CalendarInvite = None }

    let private sendNewlyCreatedEventMail createEditUrl (event: Event) =
        result {
            let mail =
                createEmail createEditUrl event
            yield Service.sendMail mail
        }

    let createEvent createEditUrl event =
        result {
            let! newEvent = repo.create event

            yield sendNewlyCreatedEventMail createEditUrl newEvent

            return newEvent
        }

    let updateEvent id event =
        result {
            let! events = repo.read
            
            let! oldEvent =
                events
                |> queryEventBy id
                |> ignoreContext
            
            return repo.update event oldEvent
        }

    let deleteEvent id =
        result {
            let! events = repo.read

            let! event =
                events
                |> queryEventBy id
                |> ignoreContext
                
            repo.del event

            return eventSuccessfullyDeleted id
        }
