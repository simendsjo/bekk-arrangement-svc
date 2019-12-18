namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open Database
open CustomErrorMessage
open ResultComputationExpression

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

    type Repo<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table> =
        { create: ('key -> Result<'domainModel, CustomErrorMessage list>) -> HttpContext -> Result<'domainModel, CustomErrorMessage list>
          update: 'domainModel -> 'dbModel -> 'domainModel
          del: 'dbModel -> Unit
          read: HttpContext -> Result<'table, CustomErrorMessage list> }

    let commitTransaction (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().SubmitUpdates()
    let rollbackTransaction (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().ClearUpdates()

    let from (models: Models<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table>): Repo<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table> =
        { create =
              fun createDomainModel ->
                  result {
                      for row in models.table
                                 >> models.create
                                 >> Ok do
                      let! newThing =
                            row
                            |> models.key
                            |> createDomainModel
                      models.updateDbWithDomain row newThing |> ignore
                      yield commitTransaction
                      let! newlyCreatedThing =
                        models.key row |> createDomainModel
                      return newlyCreatedThing
                  }

          read = models.table >> Ok
          update =
              fun newEvent event ->
                  models.updateDbWithDomain event newEvent |> ignore
                  event |> models.dbToDomain
          del = fun row -> models.delete row }
