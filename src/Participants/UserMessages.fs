namespace ArrangementService.Participants

open ArrangementService
open UserMessage

module UserMessages =
    let participantNotFound email: UserMessage =
        sprintf "Kan ikke finne deltaker %A" email
    let participationNotFound (eventId, email): UserMessage =
        sprintf "Kan ikke finne deltaker %A for arrangement %A" email eventId
    let participationSuccessfullyDeleted (eventId, email): UserMessage =
        sprintf "Deltakelse for %A på arrangement %A ble slettet" email eventId
