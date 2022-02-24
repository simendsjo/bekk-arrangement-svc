namespace ArrangementService

open System.Threading
open ArrangementService.ResultComputationExpression
open Dapper.FSharp.MSSQL
open Microsoft.AspNetCore.Http
open System.Data
open System.Data.SqlClient

open Config
open UserMessage
open FSharp.Control.Tasks.Affine

module Database =
    
    let logTransactionId transaction =
        "transaction_id", transaction.GetHashCode().ToString()
    
    let createConnection (ctx: HttpContext) =
        let config = getConfig ctx
        let connection = new SqlConnection(config.databaseConnectionString) :> IDbConnection
        connection.Open()
        let transaction = connection.BeginTransaction(IsolationLevel.Serializable)

        config.currentConnection <- connection
        config.currentTransaction <- transaction
                
        ctx |> Logging.log "Transaction started" [logTransactionId transaction] |> ignore

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

        try
            t.Commit()
        finally
            c.Close()
        
        ctx |> Logging.log "Transaction committed" [logTransactionId t] |> ignore

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

        try
            t.Rollback()
        finally
            c.Close()
                
        ctx |> Logging.log "Transaction aborted" [logTransactionId t] |> ignore

        ()

    let runSelectQuery<'t> query =
        result {
            let! config = getConfig >> Ok >> Task.wrap
            return! config.currentConnection.SelectAsync<'t>(query, config.currentTransaction) |> Task.map Ok
        }

    let runInnerJoinSelectQuery<'a, 'b> query =
        result {
            let! config = getConfig >> Ok >> Task.wrap
            return! config.currentConnection.SelectAsync<'a, 'b>(query, config.currentTransaction) |> Task.map Ok
        }

    let runInnerJoinJoinSelectQuery<'a, 'b, 'c> query =
        result {
            let! config = getConfig >> Ok >> Task.wrap
            return! config.currentConnection.SelectAsync<'a, 'b, 'c>(query, config.currentTransaction) |> Task.map Ok
        }

    let runOuterJoinSelectQuery<'a, 'b> query =
        result {
            let! config = getConfig >> Ok >> Task.wrap
            return! config.currentConnection.SelectAsyncOption<'a, 'b>(query, config.currentTransaction) |> Task.map Ok
        }

    let runOuterJoinJoinSelectQuery<'a, 'b, 'c> query =
        result {
            let! config = getConfig >> Ok >> Task.wrap
            return! config.currentConnection.SelectAsyncOption<'a, 'b, 'c>(query, config.currentTransaction) |> Task.map Ok
        }

    let runUpdateQuery query =
        result {
            let! config = getConfig >> Ok >> Task.wrap
            return! config.currentConnection.UpdateAsync(query, config.currentTransaction) |> Task.map Ok
        }

    let runDeleteQuery query =
        result {
            let! config = getConfig >> Ok >> Task.wrap
            return! config.currentConnection.DeleteAsync(query, config.currentTransaction) |> Task.map Ok
        }

    let runInsertQuery query =
        result {
            let! config = getConfig >> Ok >> Task.wrap

            // TODO: Sjekk retur verdi og returner basert på det
            let! res = config.currentConnection.InsertAsync(query, config.currentTransaction) |> Task.map Ok |> ignoreContext

            return ()
        }
