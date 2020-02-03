namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open ResultComputationExpression
open UserMessage

module Auth =

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
            for user in (fun (ctx: HttpContext) -> ctx.User |> Ok) do
                if user.HasClaim(permissionKey, permission) then
                    return ()
                else
                    return! [ AccessDenied
                                  (sprintf
                                      "Missing permission <%s> in token at '%s'"
                                       permission permissionKey) ] |> Error
        }

    let isAdmin =
        result {
            for config in (fun (ctx: HttpContext) ->
            ctx.GetService<AppConfig>() |> Ok) do

                for isAdmin in isAuthorized config.permissionsAndClaimsKey
                                   config.adminPermissionClaim do
                    return isAdmin
        }
