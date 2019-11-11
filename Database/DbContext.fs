namespace kaSkjerSvc

open FSharp.Data.Sql

module Database = 
    let [<Literal>] ConnectionString = "Server=localhost,1433;User=sa;Password=<YourStrong!Passw0rd>;Database=arrangement-db"
    let [<Literal>] DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER
    let [<Literal>] ResolutionPath = "./packages/System.Data.SqlClient/lib/netcoreapp2.1"

    type ArrangementSql = SqlDataProvider<ConnectionString=ConnectionString, DatabaseVendor=DatabaseVendor, ResolutionPath=ResolutionPath>

    let dbContext = ArrangementSql.GetDataContext()
