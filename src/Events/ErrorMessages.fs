namespace ArrangementService.Events

open Giraffe

module ErrorMessages =
    let eventNotFound id = [ sprintf "Kan ikke finne event %O" id ]
    let cantUpdateEvent id = [ sprintf "Kan ikke oppdatere event %O" id ] 
    let eventSuccessfullyDeleted id = sprintf "Event %O blei sletta" id
    let badRequest errorMessages = errorMessages 

