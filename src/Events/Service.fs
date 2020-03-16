namespace ArrangementService.Event

open ArrangementService
open ArrangementService.Email
open Models
open CalendarInvite
open ResultComputationExpression
open Queries
open UserMessages
open ArrangementService.DomainModels
open ArrangementService.Config

module Service =

    let repo = Repo.from models

    let getEvents =
        result {
            for events in repo.read do
                return Seq.map models.dbToDomain events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            for events in repo.read do

                let eventsByOrganizer =
                    queryEventsOrganizedBy organizerEmail events
                return Seq.map models.dbToDomain eventsByOrganizer
        }

    let getEvent id =
        result {
            for events in repo.read do

                let! event = events |> queryEventBy id
                return models.dbToDomain event
        }

    let private createdEventMessage createEditUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du har nå opprettet %s." event.Title.Unwrap
          sprintf "Her er en unik lenke for å endre arrangementet: %s."
              (createEditUrl event)
          "Ikke del denne med andre🕵️" ]
        |> String.concat "\n"

    let private createEmail createEditUrl fromMail (event: Event) =
        let message = createdEventMessage createEditUrl event
        { Subject = sprintf "Du opprettet %s" event.Title.Unwrap
          Message = message
          From = fromMail
          To = event.OrganizerEmail
          CalendarInvite = None }

    let private sendNewlyCreatedEventMail createEditUrl (event: Event) =
        result {
            for config in getConfig >> Ok do
                let mail =
                    createEmail createEditUrl
                        (EmailAddress config.noReplyEmail) event
                yield Service.sendMail mail
        }

    let createEvent createEditUrl event =
        result {
            for newEvent in repo.create event do

                yield sendNewlyCreatedEventMail createEditUrl newEvent

                return newEvent
        }

    let updateEvent id event =
        result {
            for events in repo.read do
                let! oldEvent = events |> queryEventBy id
                if (oldEvent.MaxParticipants < event.MaxParticipants.Unwrap) then
                    yield () |> ignore // Send mail til deltakere om fått plass
                return repo.update event oldEvent
        }

    let deleteEvent id =
        result {
            for events in repo.read do

                let! event = events |> queryEventBy id
                repo.del event

                return eventSuccessfullyDeleted id
        }
