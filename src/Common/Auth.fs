namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

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

    let isAdmin failure =
        fun next (ctx: HttpContext) ->
            let config = ctx.GetService<AppConfig>()
            hasPermission config.permissionsAndClaimsKey config.adminPermissionClaim failure next ctx

    let isLoggedIn failure =
        fun next (ctx: HttpContext) -> 
            let config = ctx.GetService<AppConfig>()
            hasPermission config.permissionsAndClaimsKey config.readPermissionClaim failure next ctx

    let makeSureUserIsAdmin: HttpHandler =
            isAdmin (accessDenied "Access Denied, you do not have admin permissions")
    let makeSureUserIsLoggedIn: HttpHandler =
            isLoggedIn (accessDenied "Access Denied, you need to be logged in to perform this action")