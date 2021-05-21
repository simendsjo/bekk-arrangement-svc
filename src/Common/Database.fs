namespace ArrangementService

open Dapper.FSharp.MSSQL
open Microsoft.AspNetCore.Http
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