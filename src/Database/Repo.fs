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
          writeToDomain: 'key -> 'WriteModel -> Result<'DomainModel, CustomErrorMessage list> }

    type Repo<'db, 'd, 'v, 'w, 'k, 't> =
        { create: ('k -> Result<'d, CustomErrorMessage list>) -> HttpContext -> Result<'d, CustomErrorMessage list>
          update: 'd -> 'db -> 'd
          del: 'db -> Unit
          read: HttpContext -> Result<'t, CustomErrorMessage list> }

    let save (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().SubmitUpdates()
    
    let commitTransaction ctx =
        save ctx

    let from (models: Models<'db, 'd, 'v, 'w, 'k, 't>): Repo<'db, 'd, 'v, 'w, 'k, 't> =
        { create =
              fun createRow ctx ->
                  let row = models.table ctx |> models.create
                  models.key row |> createRow
                  |> Result.bind
                      (fun newEvent -> 
                        models.updateDbWithDomain row newEvent |> ignore
                        save ctx
                        models.key row |> createRow)

          read = models.table >> Ok
          update =
              fun newEvent event ->
                  models.updateDbWithDomain event newEvent |> ignore
                  event |> models.dbToDomain
          del = fun row -> models.delete row }
