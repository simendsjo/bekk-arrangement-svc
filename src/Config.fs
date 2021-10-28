namespace ArrangementService

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
      mutable currentConnection: IDbConnection
      mutable currentTransaction: IDbTransaction
      mutable log: (string * string) seq
    }

module Config =
    let getConfig (context: HttpContext) =
        context.GetService<AppConfig>()



