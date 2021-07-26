namespace ArrangementService.Event

open Giraffe

open ArrangementService
open ArrangementService.DomainModels
open Auth
open ResultComputationExpression
open UserMessage
open Http
open DateTime
open System

module Authorization =

    let userIsOrganizer (event: DomainModels.Event) =
        result {
            let! userId = getUserId >> Ok

            let isTheOrganizer = userId = Some event.OrganizerId.Unwrap

            if isTheOrganizer then
                return ()
            else
                return!
                    [ AccessDenied
                          $"Du prøver å endre på et arrangement (id {event.Id.Unwrap}) som du ikke er arrangør av" ]
                    |> Error
        }

    let userHasCorrectEditToken (event: DomainModels.Event) =
        result {
            let! editToken = queryParam "editToken"

            let hasCorrectEditToken = editToken = event.EditToken.ToString()

            if hasCorrectEditToken then
                return ()
            else
                return!
                    [ AccessDenied $"Du prøvde å gjøre endringer på et arrangement (id {event.Id.Unwrap}) med ugyldig editToken" ]
                    |> Error
        }


    let userCanEditEvent eventId =
        result {
            let! event = Service.getEvent (Event.Id eventId)

            let! authResult =
                anyOf [ isAdmin
                        userHasCorrectEditToken event
                        userIsOrganizer event ]

            return authResult
        }


    let userCanSeeParticipants = userCanEditEvent

    let eventHasOpenedForRegistration (event: DomainModels.Event) =
        let openDateTime =
            DateTimeOffset.FromUnixTimeMilliseconds event.OpenForRegistrationTime.Unwrap

        if openDateTime <= DateTimeOffset.Now then
            Ok()
        else
            Error [ AccessDenied $"Arrangementet åpner for påmelding {openDateTime.ToLocalTime}" ]

    let eventHasNotPassed (event: DomainModels.Event) =
        if event.EndDate > now () then
            Ok()
        else
            Error [ AccessDenied "Arrangementet har allerede funnet sted" ]


    let eventIsExternal (eventId: Key) =
        result {
            let! event = Service.getEvent (Event.Id eventId)

            if event.IsExternal then
                return ()
            else
                return! Error [ AccessDenied "Arrangementet er internt" ]
        }

    let eventIsExternalOrUserIsAuthenticated (eventId: Key) =
        anyOf [ eventIsExternal eventId
                isAuthenticated ]
