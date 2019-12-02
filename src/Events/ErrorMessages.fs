namespace ArrangementService.Events

open Giraffe

module ErrorMessages =
    let eventNotFound id = sprintf "Kan ikke finne event %d" id |> RequestErrors.NOT_FOUND
    let cantUpdateEvent id = sprintf "Kan ikke oppdatere event %d" id |> RequestErrors.BAD_REQUEST
    let eventSuccessfullyDeleted id = sprintf "Event %d blei sletta" id |> Ok
    let badRequest id errorMessage = errorMessage |> RequestErrors.BAD_REQUEST
