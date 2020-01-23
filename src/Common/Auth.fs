namespace ArrangementService

open Giraffe

module Auth =

    let rec anyOf these orElse =
        fun next ctx ->
            let failureHandler rest _ _ = anyOf rest orElse next ctx
            match these with
            | [] -> orElse earlyReturn ctx
            | authorize::rest -> authorize (failureHandler rest) next ctx

    let accessDenied problemDescription = setStatusCode 403 >=> text problemDescription

    let hasPermission permissionKey permission =
        authorizeUser (fun user -> user.HasClaim (permissionKey, permission))

    let isAdmin config =
        hasPermission config.permissionsAndClaimsKey config.adminPermissionClaim
    let isLoggedIn config =
        hasPermission config.permissionsAndClaimsKey config.readPermissionClaim

    let makeSureUserIsAdmin: HttpHandler =
        fun next ctx ->
            isAdmin
                (ctx.GetService<AppConfig>())
                (accessDenied "Access Denied, you do not have admin permissions") next ctx
    let makeSureUserIsLoggedIn: HttpHandler =
        fun next ctx ->
            isLoggedIn
                (ctx.GetService<AppConfig>())
                (accessDenied "Access Denied, you need to be logged in to perform this action") next ctx
