namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open Dapper.FSharp
open Dapper.FSharp.MSSQL

open ArrangementService
open UserMessage
open System.Data
open System.Data.SqlClient
open Config

module Database =
    let createConnection (ctx: HttpContext) = 
        let config = getConfig ctx
        let connection = new SqlConnection(config.databaseConnectionString) :> IDbConnection
        connection.Open()
        config.currentConnection <- connection
        let transaction = connection.BeginTransaction(IsolationLevel.Serializable)
        config.currentTransaction <- transaction
        transaction

    let runSelectQuery<'t> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsync<'t>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runUpdateQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.UpdateAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runDeleteQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.DeleteAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runInsertQuery<'t, 'u> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.InsertOutputAsync<'t, 'u>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

module Http =

    type Handler<'t> = HttpContext -> Result<'t, UserMessage list>

    let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
        match condition context with
        | Ok() -> next context
        | Error errorMessage ->
            convertUserMessagesToHttpError errorMessage next context

    let handle (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        let transaction = Database.createConnection context
        match endpoint context with
        | Ok result ->
            transaction.Commit()
            json result next context
        | Error errorMessage ->
            transaction.Rollback()
            convertUserMessagesToHttpError errorMessage next context

    let getBody<'WriteModel> (context: HttpContext): Result<'WriteModel, UserMessage list>
        =
        try
            Ok(context.BindJsonAsync<'WriteModel>().Result)
        with _ -> Error [ "Feilformatert writemodel" |> BadInput ]

    let queryParam param (ctx: HttpContext) =
        ctx.GetQueryStringValue param
        |> Result.mapError
            (fun _ ->
                [ BadInput $"Missing query parameter '{param}'" ])
