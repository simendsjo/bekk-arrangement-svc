namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open System.Linq

open Database
open UserMessage
open ResultComputationExpression

module Repo =
    type Models<'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key, 'table when 'table :> IQueryable<'dbModel>> =
        { table: HttpContext -> 'table
          create: HttpContext -> 'dbModel
          delete: 'dbModel -> Unit
          key: 'dbModel -> 'key

          dbToDomain: 'dbModel -> 'DomainModel
          updateDbWithDomain: 'dbModel -> 'DomainModel -> 'dbModel
          domainToView: 'DomainModel -> 'ViewModel
          writeToDomain: 'key -> 'WriteModel -> Result<'DomainModel, UserMessage list> }

    type Repo<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table when 'table :> IQueryable<'dbModel>> =
        { create: ('key -> Result<'domainModel, UserMessage list>) -> HttpContext -> Result<'domainModel, UserMessage list>
          update: 'domainModel -> 'dbModel -> 'domainModel
          del: 'dbModel -> Unit
          read: HttpContext -> Result<'table, UserMessage list> }

    let commitTransaction (ctx: HttpContext) =
        ctx.GetService<ArrangementDbContext>().SubmitUpdates()

    let rollbackTransaction (ctx: HttpContext) =
        ctx.GetService<ArrangementDbContext>().ClearUpdates()

    let wtf keyf key table (ctx: HttpContext) =
        query {
            for row in table ctx do
                select row
        }
        |> Seq.toArray
        |> Array.tryFind (fun x -> keyf x = key)
        |> function
        | Some x -> Ok x
        | None -> Error []

    let from (models: Models<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table>): Repo<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table> =
        { create =
              fun createDomainModel ->
                  result {
                      for row in models.create >> Ok do
                          let! newThing = row
                                          |> models.key
                                          |> createDomainModel
                          models.updateDbWithDomain row newThing |> ignore
                          yield commitTransaction
                          for lol in wtf models.key (models.key row)
                                         models.table do
                              return models.dbToDomain lol
                  }

          read = models.table >> Ok
          update =
              fun newRow row ->
                  models.updateDbWithDomain row newRow |> ignore
                  row |> models.dbToDomain
          del = fun row -> models.delete row }
