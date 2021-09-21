namespace ArrangementService.Participant

open Giraffe

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage
open Http

module Authorization =

    let userHasCancellationToken (eventId, email) =
        taskResult {
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
                return! [ AccessDenied "You cannot delete your participation without your cancellation token" ] |> Error |> Task.unit
        }

    let userCanCancel eventIdAndEmail =
        anyOf
            [ userHasCancellationToken eventIdAndEmail
              isAdmin ]

    let eventHasAvailableSpots (event:DomainModels.Event) =
        taskResult {
            let hasWaitingList = event.HasWaitingList
            let maxParticipants = event.MaxParticipants.Unwrap
            let! participants = Service.getParticipantsForEvent event
            
            if hasWaitingList || maxParticipants.IsNone || maxParticipants.IsSome && participants.attendees |> Seq.length < maxParticipants.Value
            then
                return ()
            else
                return! [ AccessDenied "Arrangementet er fullt" ] |> Error |> Task.unit
        }
    let eventIsNotCancelled (event:DomainModels.Event) =  
        taskResult {
            if event.IsCancelled then
                return!
                    Error [AccessDenied "Arrangementet er avlyst"]
                    |> Task.unit
            else
                return ()
        }

    let oneCanParticipateOnEvent eventIdKey =
        taskResult {
            let eventId = Event.Id eventIdKey
            let! event = Service.getEvent eventId
            do! eventHasAvailableSpots event
            do! Event.Authorization.eventHasNotPassed event 
            do! Event.Authorization.eventHasOpenedForRegistration event 
            do! eventIsNotCancelled event 
            do! Event.Authorization.eventIsExternalOrUserIsAuthenticated eventIdKey 
        }
