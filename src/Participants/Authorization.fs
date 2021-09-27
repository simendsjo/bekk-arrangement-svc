namespace ArrangementService.Participant

open Giraffe

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage
open Http
open System

module Authorization =

    let userHasCancellationToken (eventId, email) =
        result {
            let! cancellationToken = queryParam "cancellationToken"

            let! participant = Service.getParticipant
                                   (Event.Id eventId,
                                    Email.EmailAddress email)

            let hasCorrectCancellationToken =
                cancellationToken =
                    participant.CancellationToken.ToString()

            if hasCorrectCancellationToken then
                return ()
            else
                return! [ AccessDenied "You cannot delete your participation without your cancellation token" ] |> Error |> Task.wrap
        }

    let userCanCancel eventIdAndEmail =
        anyOf
            [ userHasCancellationToken eventIdAndEmail
              isAdmin ]

    let eventHasAvailableSpots (event:DomainModels.Event) =
        result {
            let hasWaitingList = event.HasWaitingList
            let maxParticipants = event.MaxParticipants.Unwrap
            let! numberOfParticipants = Service.getNumberOfParticipantsForEvent event.Id
            
            if hasWaitingList || maxParticipants.IsNone || numberOfParticipants.Unwrap < maxParticipants.Value
            then
                return ()
            else
                return! [ AccessDenied "Arrangementet er fullt" ] |> Error |> Task.wrap
        }

    let eventIsNotCancelled (event:DomainModels.Event) =  
        result {
            if event.IsCancelled then
                return!
                    Error [AccessDenied "Arrangementet er avlyst"]
                    |> Task.wrap
            else
                return ()
        }

    let oneCanParticipateOnEvent eventIdKey =
        result {
            let eventId = Event.Id eventIdKey
            let! event = Service.getEvent eventId
            do! eventHasAvailableSpots event
            do! Event.Authorization.eventHasNotPassed event 
            do! Event.Authorization.eventIsOpenForRegistration event 
            do! eventIsNotCancelled event 
            do! Event.Authorization.eventIsExternalOrUserIsAuthenticatedEvent event 
        }
