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
            for cancellationToken in queryParam "cancellationToken" do

                for participant in Service.getParticipant
                                       (Event.Id eventId,
                                        Email.EmailAddress email) do

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
            for event in Event.Service.getEvent (Event.Id eventId) do
                let hasWaitingList = event.HasWaitingList
                let maxParticipants = event.MaxParticipants.Unwrap
                for participants in Service.getParticipantsForEvent event do
                    if hasWaitingList || maxParticipants = 0
                       || participants.attendees
                          |> Seq.length < maxParticipants then
                        return ()
                    else
                        return! [ AccessDenied "The event is full" ] |> Error
        }
