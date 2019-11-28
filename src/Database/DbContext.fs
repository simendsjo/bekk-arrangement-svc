namespace ArrangementService

open FSharp.Data.Sql

module Database =
    [<Literal>]
    let ConnectionString = "Data Source=rds-dev.bekk.local;Database=arrangement-db;User ID=event-svc;Password=L8v*vx8BPSBC"
    // "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"

    [<Literal>]
    let DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER

    [<Literal>]
    let ResolutionPath = "~/.nuget/packages/system.data.sqlclient/4.7.0/lib/netcoreapp2.1"

    type ArrangementSql = SqlDataProvider<ContextSchemaPath="DbSchema.xml", ConnectionString=ConnectionString, DatabaseVendor=DatabaseVendor, ResolutionPath=ResolutionPath>

    type ArrangementDbContext = ArrangementSql.dataContext

    let createDbContext (connectionString: string): ArrangementSql.dataContext =
        ArrangementSql.GetDataContext(connectionString)
