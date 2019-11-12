namespace arrangementSvc

open FSharp.Data.Sql

module Database =
    [<Literal>]
    let ConnectionString = "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"

    //                     "Server=rds-dev.bekk.local;User=event-svc;Password=9U6&i5xvvBkG%Bs*S&eT;Database=event-svc"

    [<Literal>]
    let DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER

    [<Literal>]
    let ResolutionPath = "./packages/System.Data.SqlClient/lib/netcoreapp2.1"

    type ArrangementSql = SqlDataProvider<ContextSchemaPath="DbSchema.xml", ConnectionString=ConnectionString, DatabaseVendor=DatabaseVendor, ResolutionPath=ResolutionPath>

    type ArrangementDbContext = ArrangementSql.dataContext

    let createDbContext (connectionString: string): ArrangementSql.dataContext =
        ArrangementSql.GetDataContext(connectionString)
;