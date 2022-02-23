namespace ArrangementService

open System.Data.SqlClient
open Microsoft.AspNetCore.Http
open Giraffe
open System.Data

type AppConfig =
    { isProd: bool
      requestId: string
      userIdClaimsKey: string
      permissionsAndClaimsKey: string
      adminPermissionClaim: string
      readPermissionClaim: string
      sendMailInDevEnvWhiteList: string list
      noReplyEmail: string
      databaseConnectionString: string
      mutable currentConnection: SqlConnection
      mutable currentTransaction: IDbTransaction
      mutable log: (string * string) seq
    }

module Config =
    let getConfig (context: HttpContext) =
        context.GetService<AppConfig>()



