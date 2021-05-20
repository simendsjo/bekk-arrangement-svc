namespace ArrangementService

open FSharp.Data.Sql

module Database =
    [<Literal>]
    let ConnectionString =
        "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
