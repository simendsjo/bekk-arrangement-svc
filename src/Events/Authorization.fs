namespace ArrangementService.Event

open Giraffe

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage
open Http
open DateTime
open System

module Authorization =

    let userCreatedEvent eventId =
        result {
            let! editToken = queryParam "editToken"

            let! event = Service.getEvent (Id eventId)

            let hasCorrectEditToken =
                editToken = event.EditToken.ToString()

            if hasCorrectEditToken then
                return ()
            else
                return! [ AccessDenied
                              (sprintf
                                  "You are trying to edit an event (id %O) which you did not create"
                                   eventId) ] |> Error
        }

    let userCanEditEvent eventId =
        anyOf
            [ userCreatedEvent eventId
              isAdmin ]

    let eventHasOpenedForRegistration eventId =
        result {
            let! event = Service.getEvent (Id eventId)
            let openDateTime =
                DateTimeOffset.FromUnixTimeMilliseconds
                    event.OpenForRegistrationTime.Unwrap

            if openDateTime <= DateTimeOffset.Now then
                return ()
            else
                return! [ AccessDenied
                              (sprintf
                                  "Arrangementet åpner for påmelding %A"
                                   openDateTime) ] |> Error
        }

    let eventHasNotPassed eventId =
        result {
            let! event = Service.getEvent (Id eventId)
            if event.EndDate > now() then
                return ()
            else
                return! [ AccessDenied
                              "Arrangementet har allerede funnet sted" ]
                        |> Error
        }
