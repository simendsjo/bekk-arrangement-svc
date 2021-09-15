namespace ArrangementService

open Dapper.FSharp.MSSQL
open Microsoft.AspNetCore.Http
open System.Data
open System.Data.SqlClient

open Config

module Database =

    (*
        `createConnection` is idempotent, i.e. you can call it many times
        and still get the same transaction.

        This is useful because one (http) request does potentially many "check"s
        before doing a "handle" of the endpoint. You really want the "are there
        available spots on Event" to be evaluated in the same transaction as the
        "add Participant to Event" operation.
    *)
    let createConnection (ctx: HttpContext) = 
        let config = getConfig ctx
        let connection = new SqlConnection(config.databaseConnectionString) :> IDbConnection
        connection.Open()
        let transaction = connection.BeginTransaction(IsolationLevel.Serializable)

        config.currentConnection <- connection
        config.currentTransaction <- transaction

        ()

    let commitTransaction (ctx: HttpContext) = 
        let config = getConfig ctx

        let t = config.currentTransaction
        let c = config.currentConnection
        if isNull t <> isNull c then
            raise (System.Exception "Dobbel commit bug")
        else

        config.currentTransaction <- null
        config.currentConnection <- null

        t.Commit()
        c.Close()

        ()

    let rollbackTransaction (ctx: HttpContext) = 
        let config = getConfig ctx

        let t = config.currentTransaction
        let c = config.currentConnection
        if isNull t <> isNull c then
            raise (System.Exception "Dobbel commit bug")
        else

        config.currentTransaction <- null
        config.currentConnection <- null

        t.Rollback()
        c.Close()

        ()

    let runSelectQuery<'t> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsync<'t>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runInnerJoinSelectQuery<'a, 'b> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsync<'a, 'b>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runInnerJoinJoinSelectQuery<'a, 'b, 'c> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsync<'a, 'b, 'c>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runOuterJoinSelectQuery<'a, 'b> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsyncOption<'a, 'b>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runOuterJoinJoinSelectQuery<'a, 'b, 'c> (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsyncOption<'a, 'b, 'c>(query, config.currentTransaction)
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

    let runInsertQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.InsertAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore
        // TODO: Sjekk retur verdi og returner basert på det

        Ok ()