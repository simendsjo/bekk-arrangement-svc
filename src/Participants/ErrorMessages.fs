namespace ArrangementService.Participants

open ArrangementService
open CustomErrorMessage

module ErrorMessages =
    let participantNotFound email: CustomErrorMessage = sprintf "Kan ikke finne deltaker %A" email
    let participationNotFound (eventId, email): CustomErrorMessage =
        sprintf "Kan ikke finne deltaker %A for arrangement %A" email eventId
    let participationSuccessfullyDeleted (eventId, email): CustomErrorMessage =
        sprintf "Deltakelse for %A på arrangement %A ble slettet" email eventId
