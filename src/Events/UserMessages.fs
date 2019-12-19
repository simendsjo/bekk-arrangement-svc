namespace ArrangementService.Events

open ArrangementService
open UserMessage

module UserMessages =
    let eventNotFound id: UserMessage =
        sprintf "Kan ikke finne event %O" id
    let cantUpdateEvent id: UserMessage =
        sprintf "Kan ikke oppdatere event %O" id
    let eventSuccessfullyDeleted id: UserMessage =
        sprintf "Event %O blei sletta" id
