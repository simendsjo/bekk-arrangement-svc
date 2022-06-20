module Database 

open System
open Dapper
open Microsoft.Data.SqlClient

type DatabaseConnection (connectionString: string) =
    let connection: SqlConnection = new SqlConnection(connectionString)
    member this.getConnection () =
        connection.Open()
        connection
    interface IDisposable with
        member __.Dispose() =
            connection.Close()
            connection.Dispose()