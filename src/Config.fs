namespace ArrangementService

open Microsoft.AspNetCore.Http
open Giraffe

type AppConfig =
    { isProd: bool
      userIdClaimsKey: string
      permissionsAndClaimsKey: string
      adminPermissionClaim: string
      readPermissionClaim: string
      sendMailInDevEnvWhiteList: string list
      noReplyEmail: string }

module Config =
    let getConfig (context: HttpContext) =
        context.GetService<AppConfig>()
