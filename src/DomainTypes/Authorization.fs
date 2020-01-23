namespace ArrangementService

open Giraffe

open Auth

module Authorization =

    let userCreatedEvent eventId onFail =
        fun next ctx ->
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
