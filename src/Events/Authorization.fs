namespace ArrangementService.Event

open Giraffe

open ArrangementService
open Auth
open UserMessage

module Authorization =

    let userCreatedEvent eventId ctx =
        // Denne trenger nok en ekte implementasjon etterhvert
        [ AccessDenied
            (sprintf
                "You are trying to edit an event (id %O) which you did not create"
                 eventId) ] |> Error

    let userCanEditEvent eventId =
        anyOf
            [ isAdmin
              userCreatedEvent eventId ]
