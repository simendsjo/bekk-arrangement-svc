namespace ArrangementService

open FSharp.Data.Sql
open Microsoft.EntityFrameworkCore.Storage
open Microsoft.EntityFrameworkCore.Storage.Internal

module Database =
    [<Literal>]
    let ConnectionString = "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"

    [<Literal>]
    let DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER

    [<Literal>]
    let ResolutionPath = "./packages/System.Data.SqlClient/lib/netcoreapp2.1"

    type ArrangementSql = SqlDataProvider<ContextSchemaPath="DbSchema.xml", ConnectionString=ConnectionString, DatabaseVendor=DatabaseVendor, ResolutionPath=ResolutionPath>

    type ArrangementDbContext = ArrangementSql.dataContext

    let createDbContext (connectionString: string): ArrangementSql.dataContext =
        ArrangementSql.GetDataContext(connectionString)


            