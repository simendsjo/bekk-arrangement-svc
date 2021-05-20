namespace ArrangementService.Event

open ArrangementService
open ArrangementService.Email
open Models
open ResultComputationExpression
open UserMessages
open ArrangementService.DomainModels
open Http

module Service =

    let getEvents: Handler<Event seq> =
        result {
            let! events = Queries.getEvents >> Ok
            return Seq.map dbToDomain events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! events = Queries.getEvents >> Ok

            let eventsByOrganizer =
                Queries.queryEventsOrganizedBy organizerEmail events
            return Seq.map dbToDomain eventsByOrganizer
        }

    let getEvent id =
        result {
            let! events = Queries.getEvents >> Ok

            let! event =
                events
                |> Queries.queryEventBy id
                |> ignoreContext
                
            return dbToDomain event
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
            let! newEvent = Queries.createEvent event

            yield sendNewlyCreatedEventMail createEditUrl newEvent

            return newEvent
        }

    let updateEvent id event =
        result {
            do! Queries.updateEvent id event
            return event 
        }

    let deleteEvent id =
        result {
            do! Queries.deleteEvent id
            return eventSuccessfullyDeleted id
        }
