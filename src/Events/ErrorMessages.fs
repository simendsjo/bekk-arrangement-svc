namespace ArrangementService.Events

open Giraffe

module ErrorMessages =
    let eventNotFound id = sprintf "Kan ikke finne event %O" id |> RequestErrors.NOT_FOUND
    let cantUpdateEvent id = sprintf "Kan ikke oppdatere event %O" id |> RequestErrors.BAD_REQUEST
    let eventSuccessfullyDeleted id = sprintf "Event %O blei sletta" id |> Ok
    let badRequest id errorMessage = errorMessage |> RequestErrors.BAD_REQUEST
