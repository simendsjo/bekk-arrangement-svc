namespace ArrangementService.Events

open ArrangementService
open UserMessage

module UserMessages =
    let eventNotFound id: UserMessage =
        sprintf "Kan ikke finne event %O" id |> NotFound
    let cantUpdateEvent id: UserMessage =
        sprintf "Kan ikke oppdatere event %O" id |> NotFound
    let eventSuccessfullyDeleted id: string =
        sprintf "Event %O blei sletta" id 
