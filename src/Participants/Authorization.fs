namespace ArrangementService.Participant

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage

module Authorization =

    let queryParam param (ctx: HttpContext) =
        ctx.GetQueryStringValue param
        |> Result.mapError
            (fun _ ->
                [ BadInput(sprintf "Missing query parameter '%s'" param) ])

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
            [ isAdmin
              userHasCancellationToken eventIdAndEmail ]

    let eventHasAvailableSpots eventId =
        result {
            for event in Event.Service.getEvent (Event.Id eventId) do
                let maxParticipants = event.MaxParticipants.Unwrap
                for participants in Service.getParticipantsForEvent
                                        (Event.Id eventId) do
                    if maxParticipants = 0 || participants
                                              |> Seq.length < maxParticipants then
                        return ()
                    else
                        return! [ AccessDenied "The event is full" ] |> Error
        }
