namespace ArrangementService.Event

open ArrangementService
open UserMessage

module UserMessages =
    let eventNotFound id: UserMessage =
        $"Kan ikke finne event {id}" |> NotFound
    let cantUpdateEvent id: UserMessage =
        $"Kan ikke oppdatere event {id}" |> NotFound
    let eventSuccessfullyDeleted id: string =
        $"Event {id} blei sletta"
