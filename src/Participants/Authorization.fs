namespace ArrangementService.Participant

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open Auth

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
        fun next (ctx: HttpContext) ->
            let x = Event.Service.getEvent (Event.Id eventId) ctx
            match x with
            | Ok event ->
                let maxParticipants = event.MaxParticipants.Unwrap
                let participants =
                    Service.getParticipantsForEvent (Event.Id eventId) ctx
                match participants with
                | Ok ps ->
                    if maxParticipants = 0 || ps
                                              |> Seq.length < maxParticipants then
                        next ctx
                    else
                        accessDenied "The event is full" earlyReturn ctx
                | Error _ ->
                    (serverError "Participants lookup has failed") earlyReturn
                        ctx
            | Error _ ->
                notFound (sprintf "Event with id %O not found" eventId)
                    earlyReturn ctx
