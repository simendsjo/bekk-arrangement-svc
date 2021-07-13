namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open ResultComputationExpression
open UserMessage


module Auth =

    let employeeIdClaim = "https://api.bekk.no/claims/employeeId"

    let anyOf these =
        fun ctx ->
            let auths =
                these |> List.map (fun authorize -> authorize ctx)
            if auths |> List.contains (Ok()) then
                Ok()
            else
                auths
                |> List.collect (function
                    | Ok() -> []
                    | Error errors -> errors)
                |> Error

    let private isAuthorized permissionKey permission =
        result {
            let! user = (fun (ctx: HttpContext) -> ctx.User |> Ok)
            if user.HasClaim(permissionKey, permission) then
                return ()
            else
                return! [ AccessDenied $"Missing permission <{permission}> in token at '{permissionKey}'" ] |> Error
        }



    let isAuthenticated =
        result {
            let! user = (fun (ctx: HttpContext) -> ctx.User |> Ok)
            if user.Identity.IsAuthenticated then
                return ()
            else
                return! [ NotLoggedIn $"User not logged in" ] |> Error
        }

    let isAdmin =
        result {
            do! isAuthenticated
            let! config = (fun (ctx: HttpContext) -> ctx.GetService<AppConfig>() |> Ok)

            let! isAdmin = isAuthorized config.permissionsAndClaimsKey config.adminPermissionClaim
            return isAdmin
        }
    

    let getUserId (ctx:HttpContext) : int option =
        isAuthenticated ctx 
            |> function
                | Error _ -> None
                | Ok _ -> ctx.User.FindFirst(employeeIdClaim).Value |> Tools.tryParseInt

    let isAuthenticatedAs id =
        result{
            let! userId = getUserId >> Option.withError [NotLoggedIn $"Could not retrieve UserId"]
            if userId = id then
                return ()
            else
                return! [AccessDenied $"User not logged in with id:{id}"] |> Error
       }

    let isAdminOrAuthenticatedAsUser id = anyOf [isAdmin
                                                 isAuthenticatedAs id]