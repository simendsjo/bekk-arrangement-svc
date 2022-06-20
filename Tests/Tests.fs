module Tests.All

open Expecto
open Microsoft.Data.SqlClient

open migrator

let allTests =
    testList "All Tests" [
        General.tests
        CreateEvent.tests
        RegisterToEvent.tests
        UpdateEvent.tests
        GetEvent.tests
        DeleteEvent.tests
    ]

[<EntryPoint>]
let main args =
    let connectionStringNoDb =
        let connectionString = App.configuration["ConnectionStrings:EventDb"]
        let cs = SqlConnectionStringBuilder(connectionString)
        cs.InitialCatalog <- ""
        cs
    async {
      printfn "Sleeping a little so container can start..."
      do! Async.Sleep(5000)
    } |> Async.RunSynchronously
    let connection = new SqlConnection(connectionStringNoDb.ConnectionString)
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- "CREATE DATABASE [arrangement-db];"
    command.ExecuteNonQuery() |> ignore
    connection.Close()
    Migrate.Run(connectionStringNoDb.ConnectionString)
    runTestsWithCLIArgs [] args allTests