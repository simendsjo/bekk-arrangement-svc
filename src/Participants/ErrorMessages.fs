namespace ArrangementService.Participants

open Giraffe

module ErrorMessages =
    let participantNotFound email = sprintf "Kan ikke finne deltaker %A" email
    let participationNotFound (eventId, email) =
        sprintf "Kan ikke finne deltaker %A for arrangement %A" email eventId
    let participationSuccessfullyDeleted (eventId, email) =
        sprintf "Deltakelse for %A på arrangement %A ble slettet" email eventId |> Ok
