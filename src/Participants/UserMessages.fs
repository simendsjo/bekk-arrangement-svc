namespace ArrangementService.Participants

open ArrangementService
open UserMessage

module UserMessages =
    let participantNotFound email: UserMessage =
        sprintf "Kan ikke finne deltaker %A" email |> NotFound
    let participationNotFound (eventId, email): UserMessage =
        sprintf "Kan ikke finne deltaker %A for arrangement %A" email eventId |> NotFound
    let participationSuccessfullyDeleted (eventId, email): string =
        sprintf "Deltakelse for %A på arrangement %A ble slettet" email eventId
