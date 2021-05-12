namespace ArrangementService.Participant

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage
open Http

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
                return! [ AccessDenied
                              "You cannot delete your participation without your cancellation token" ]
                        |> Error
        }

    let userCanCancel eventIdAndEmail =
        anyOf
            [ userHasCancellationToken eventIdAndEmail
              isAdmin ]

    let eventHasAvailableSpots eventId =
        result {
            let! event = Event.Service.getEvent (Event.Id eventId)
            
            let maxParticipants = event.MaxParticipants.Unwrap
            let! participants = Service.getParticipantsForEvent (Event.Id eventId)
            
            if maxParticipants = 0 || participants |> Seq.length < maxParticipants
            then
                return ()
            else
                return! [ AccessDenied "The event is full" ] |> Error
        }
