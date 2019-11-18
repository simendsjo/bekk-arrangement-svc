namespace ArrangementService

open Microsoft.AspNetCore.Http

module Repo =

    type Models<'table, 'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key> =
        { table: HttpContext -> 'table
          records: HttpContext -> 'DomainModel seq
          create: 'table -> 'dbModel
          delete: 'dbModel -> Unit
          key: 'dbModel -> 'key
          dbToDomain: 'dbModel -> 'DomainModel
          updateDbWithDomain: 'dbModel -> 'DomainModel -> 'dbModel
          domainToView: 'DomainModel -> 'ViewModel
          writeToDomain: 'key -> 'WriteModel -> 'DomainModel }

    let from (models: Models<'t, 'db, 'd, 'v, 'w, 'k>) =
        {| create =
               fun createRow ctx ->
                   let row = models.table ctx |> models.create
                   let newEvent = models.key row |> createRow
                   models.updateDbWithDomain row newEvent |> ignore
                   newEvent
           read = models.records
           update =
               fun newEvent event ->
                   models.updateDbWithDomain event newEvent |> ignore
                   event |> models.dbToDomain
           del = fun row -> models.delete row |}
