namespace ArrangementService.Event

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open Auth

module Authorization =

    let userCreatedEvent eventId onFail =
        fun next (ctx: HttpContext) ->
            // Inntil videre feiler vi ubetinget her.
            // Må implementere denne featuren,
            // ellers er det berre admin som kan redigere events
            onFail earlyReturn ctx

    let userCanEditEvent eventId =
        anyOf
            [ isAdmin
              userCreatedEvent eventId ]
            (accessDenied
                (sprintf
                    "You are trying to edit an event (id %O) which you did not create"
                     eventId))
