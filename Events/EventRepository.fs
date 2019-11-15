namespace ArrangementService.Events

module Repo =

    let create createRow (table: Models.TableModel) =
        let row = table.Create()
        let newEvent = Models.key row |> createRow
        Models.updateDbWithDomain row newEvent |> ignore
        newEvent

    let update newEvent event =
        Models.updateDbWithDomain event newEvent |> ignore
        event |> Models.dbToDomain

    let del (row: Models.DbModel) = row.Delete()
