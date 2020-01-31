namespace ArrangementService.Participant

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage

module Authorization =

    let userHasCancellationToken (eventId, email) onFail =
        fun next (ctx: HttpContext) ->
            let fail() = onFail earlyReturn ctx

            let cancellationToken = ctx.GetQueryStringValue "cancellationToken"

            match cancellationToken with
            | Error _ -> fail()
            | Ok cancellationToken ->

                let participant =
                    Service.getParticipant
                        (Event.Id eventId, Email.EmailAddress email) ctx

                match participant with
                | Error _ -> fail()
                | Ok participant ->

                    let hasCorrectCancellationToken =
                        cancellationToken =
                            participant.CancellationToken.ToString()

                    if hasCorrectCancellationToken then next ctx else fail()

    let userCanCancel eventIdAndEmail =
        anyOf
            [ isAdmin
              userHasCancellationToken eventIdAndEmail ]
            (accessDenied
                "You cannot delete your participation without your cancellation token")

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
