namespace ArrangementService.Events

open Giraffe

module ErrorMessages =
    let eventNotFound id = sprintf "Kan ikke finne event %A" id |> RequestErrors.NOT_FOUND
    let cantUpdateEvent id = sprintf "Kan ikke oppdatere event %A" id |> RequestErrors.BAD_REQUEST
    let eventSuccessfullyDeleted id = sprintf "Event %A blei sletta" id |> Ok
