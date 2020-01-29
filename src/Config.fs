namespace ArrangementService

type AppConfig =
    { isProd: bool
      userIdClaimsKey: string
      permissionsAndClaimsKey: string
      adminPermissionClaim: string
      readPermissionClaim: string
      sendMailInDevEnvWhiteList: string list }
