namespace arrangementSvc

open FSharp.Data.Sql
open Microsoft.Extensions.DependencyInjection

module Database =
    let [<Literal>] ConnectionString = "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
    let [<Literal>] DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER
    let [<Literal>] ResolutionPath = "./packages/System.Data.SqlClient/lib/netcoreapp2.1"

    type ArrangementSql = SqlDataProvider<ConnectionString=ConnectionString, DatabaseVendor=DatabaseVendor, ResolutionPath=ResolutionPath>

    type ArrangementDbContext = ArrangementSql.dataContext
    
    let createDbContext (connectionString : string) : ArrangementSql.dataContext = ArrangementSql.GetDataContext(connectionString);
