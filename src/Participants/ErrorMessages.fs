namespace ArrangementService.Participants

open Giraffe

module ErrorMessages =
    let participantNotFound email = sprintf "Kan ikke finne deltaker %A" email
    let participationNotFound email id =
        sprintf "Kan ikke finne deltaker %A for arrangement %A" email id
    let participationSuccessfullyDeleted email id =
        sprintf "Deltakelse for %A på arrangement %A ble slettet" email id |> Ok
