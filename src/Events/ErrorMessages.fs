namespace ArrangementService.Events

open ArrangementService
open CustomErrorMessage

module ErrorMessages =
    let eventNotFound id: CustomErrorMessage = sprintf "Kan ikke finne event %O" id
    let cantUpdateEvent id: CustomErrorMessage = sprintf "Kan ikke oppdatere event %O" id
    let eventSuccessfullyDeleted id: CustomErrorMessage = sprintf "Event %O blei sletta" id
