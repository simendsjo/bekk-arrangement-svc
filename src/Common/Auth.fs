namespace ArrangementService

open Giraffe

module Auth =

    let private permissionKey = "https://api.bekk.no/claims/permission"
    let private hasPermission permission errorMessage = authorizeUser (fun user -> user.HasClaim (permissionKey, permission)) (errorMessage permission)  

    let private accessDenied permission = setStatusCode 403 >=> text (sprintf "Access Denied, you do not have permission <%s>" permission)
    let private mustBeAdmin permission = setStatusCode 403 >=> text (sprintf "Access denies, you do not have admin permission <%s>" permission)

    let isUser: HttpHandler = hasPermission "read:arrangement" accessDenied
    let isAdmin: HttpHandler = hasPermission "admin:arrangement" mustBeAdmin
