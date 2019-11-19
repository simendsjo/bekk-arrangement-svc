namespace ArrangementService

open Microsoft.AspNetCore.Http

module Repo =

    type Models<'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key, 'table> =
        { table: HttpContext -> 'table
          create: 'table -> 'dbModel
          delete: 'dbModel -> Unit
          key: 'dbModel -> 'key
          dbToDomain: 'dbModel -> 'DomainModel
          updateDbWithDomain: 'dbModel -> 'DomainModel -> 'dbModel
          domainToView: 'DomainModel -> 'ViewModel
          writeToDomain: 'key -> 'WriteModel -> 'DomainModel }

    type Repo<'db, 'd, 'v, 'w, 'k, 't> =
        { create: ('k -> 'd) -> HttpContext -> 'd
          update: 'd -> 'db -> 'd
          del: 'db -> Unit
          read: HttpContext -> 't }

    let from (models: Models<'db, 'd, 'v, 'w, 'k, 't>): Repo<'db, 'd, 'v, 'w, 'k, 't> =
        { create =
              fun createRow ctx ->
                  let row = models.table ctx |> models.create
                  let newEvent = models.key row |> createRow
                  models.updateDbWithDomain row newEvent |> ignore
                  newEvent
          read = models.table
          update =
              fun newEvent event ->
                  models.updateDbWithDomain event newEvent |> ignore
                  event |> models.dbToDomain
          del = fun row -> models.delete row }
