namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open Database

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

    type Repo<'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key, 'table> =
        { create: ('key -> 'DomainModel) -> HttpContext -> 'DomainModel
          update: 'DomainModel -> 'dbModel -> 'DomainModel
          del: 'dbModel -> Unit
          read: HttpContext -> 'table }

    let save (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().SubmitUpdates()

    let commitTransaction x ctx = save ctx

    let from (models: Models<'db, 'd, 'v, 'w, 'k, 't>): Repo<'db, 'd, 'v, 'w, 'k, 't> =
        { create =
              fun createRow ctx ->
                  let row = models.table ctx |> models.create
                  let newEvent = models.key row |> createRow
                  models.updateDbWithDomain row newEvent |> ignore
                  save ctx
                  models.key row |> createRow

          read = models.table
          update =
              fun newEvent event ->
                  models.updateDbWithDomain event newEvent |> ignore
                  event |> models.dbToDomain
          del = fun row -> models.delete row }

    type RegModels<'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key, 'table> =
        { table: HttpContext -> 'table
          create: 'table -> 'dbModel
          key: 'dbModel -> 'key
          dbToDomain: 'dbModel -> 'DomainModel
          updateDbWithDomain: 'dbModel -> 'DomainModel -> 'dbModel
          writeToDomain: 'key -> 'WriteModel -> 'DomainModel }

    type RegRepo<'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key, 'table> =
        { create: ('key -> 'DomainModel) -> HttpContext -> 'DomainModel }

    let register (models: RegModels<'db, 'd, 'v, 'w, 'k, 't>): RegRepo<'db, 'd, 'v, 'w, 'k, 't> =
        { create =
            fun createRow ctx ->
                  let row = models.table ctx |> models.create
                  let newRegistration = models.key row |> createRow
                  models.updateDbWithDomain row newRegistration |> ignore
                  save ctx
                  models.key row |> createRow }