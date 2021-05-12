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
          updateDbWithDomain: 'dbModel -> 'DomainModel -> 'dbModel }

    type Repo<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table when 'table :> IQueryable<'dbModel>> =
        { create: ('key -> Result<'domainModel, UserMessage list>) -> HttpContext -> Result<'domainModel, UserMessage list>
          update: 'domainModel -> 'dbModel -> 'domainModel
          del: 'dbModel -> Unit
          read: HttpContext -> Result<'table, UserMessage list> }

    let commitTransaction (ctx: HttpContext) =
        ctx.GetService<ArrangementDbContext>().SubmitUpdates()

    let rollbackTransaction (ctx: HttpContext) =
        ctx.GetService<ArrangementDbContext>().ClearUpdates()

    /// We need to read out the updated database row
    /// since sqlprovider currently does not update
    /// the newly created row with anything but the
    /// primary key. Default values set by the db or
    /// timestamps and similar are not reflected in
    /// the object. This is talked about in issue
    /// https://github.com/fsprojects/SQLProvider/issues/620
    /// and this function can be removed when it is
    /// resolved. Then we can return "row" from create
    /// directly (after commitTransaction of course).
    let getNewRow key row table (ctx: HttpContext) =
        query {
            for row in table ctx do
                select row
        }
        |> Seq.toArray
        // We do a sequential read of the table here because
        // tuple comparison (for instance) can't be translated
        // to SQL in the above query.
        |> Array.tryFind (fun x -> key x = key row)
        |> function
        | Some x -> Ok x
        | None -> Error []

    let from (models: Models<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table>): Repo<'dbModel, 'domainModel, 'viewModel, 'writeModel, 'key, 'table> =
        { create =
              fun createDomainModel ->
                  result {
                      let! row = models.create >> Ok
                      let! newThing = row
                                      |> models.key
                                      |> createDomainModel
                                      |> ignoreContext
                      models.updateDbWithDomain row newThing |> ignore
                      yield commitTransaction
                      let! newRow = getNewRow models.key row models.table
                      return models.dbToDomain newRow
                  }

          read = models.table >> Ok
          update =
              fun newRow row ->
                  models.updateDbWithDomain row newRow |> ignore
                  row |> models.dbToDomain
          del = fun row -> models.delete row }
