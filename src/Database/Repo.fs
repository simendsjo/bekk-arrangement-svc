namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open Database
open Validation

module Repo =
    type Models<'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key, 'table> =
        { table: HttpContext -> 'table
          create: 'table -> 'dbModel
          delete: 'dbModel -> Unit
          key: 'dbModel -> 'key
          dbToDomain: 'dbModel -> 'DomainModel
          updateDbWithDomain: 'dbModel -> 'DomainModel -> 'dbModel
          domainToView: 'DomainModel -> 'ViewModel
          writeToDomain: 'key -> 'WriteModel -> Result<'DomainModel, CustomErrorMessage list> }

    type Repo<'db, 'd, 'v, 'w, 'k, 't> =
        { create: ('k -> Result<'d, CustomErrorMessage list>) -> HttpContext -> Result<'d, CustomErrorMessage list>
          update: 'd -> 'db -> 'd
          del: 'db -> Unit
          read: HttpContext -> 't }

    let save (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().SubmitUpdates()
    
    let commitTransaction x ctx =
        save ctx
        Ok x

    let from (models: Models<'db, 'd, 'v, 'w, 'k, 't>): Repo<'db, 'd, 'v, 'w, 'k, 't> =
        { create =
              fun createRow ctx ->
                validator {
                  let row = models.table ctx |> models.create
                  let! newEvent = models.key row |> createRow
                  models.updateDbWithDomain row newEvent |> ignore
                  save ctx
                  return! models.key row |> createRow
                }

          read = models.table
          update =
              fun newEvent event ->
                  models.updateDbWithDomain event newEvent |> ignore
                  event |> models.dbToDomain
          del = fun row -> models.delete row }
