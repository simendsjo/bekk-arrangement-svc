namespace ArrangementService

open Giraffe

open Auth

module Authorization =

    let userCreatedEvent id onFail =
        fun next ctx ->
            // Inntil videre feiler vi ubetinget her.
            // Må implementere denne featuren,
            // ellers er det berre admin som kan redigere events
            onFail earlyReturn ctx

    let userCanEditEvent id =
            anyOf [isAdmin; userCreatedEvent id]
                (accessDenied
                    (sprintf "You are trying to edit an event (id %O) which you did not create" id))
