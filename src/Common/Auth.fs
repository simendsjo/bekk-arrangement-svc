namespace ArrangementService

open Giraffe

module Auth =

    let rec anyOf these orElse =
        fun next ctx ->
            match these with
            | [] -> orElse earlyReturn ctx
            | thisOne::rest -> thisOne (fun _ _ -> anyOf rest orElse next ctx) next ctx

    let private permissionKey = "https://api.bekk.no/claims/permission"
    let adminPermission = "admin:arrangement"
    let readPermissions = "read:arrangement"

    let accessDenied problemDescription = setStatusCode 403 >=> text problemDescription

    let hasPermission permission = authorizeUser (fun user -> user.HasClaim (permissionKey, permission))

    let isAdmin = hasPermission adminPermission
    let isLoggedIn = hasPermission readPermissions

    let makeSureUserIsLoggedIn: HttpHandler = isLoggedIn (accessDenied (sprintf "Access Denied, you do not have permission <%s>" readPermissions))
    let makeSureUserIsAdmin: HttpHandler = isAdmin (accessDenied (sprintf "Access Denied, you do not have permission <%s>" adminPermission))
