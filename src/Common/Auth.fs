namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open ResultComputationExpression
open UserMessage
open FSharp.Control.Tasks.V2


module Auth =

    let employeeIdClaim = "https://api.bekk.no/claims/employeeId"

    let anyOf (these: Handler<unit> list): Handler<unit> =
        result {
            let! auths =
                fun ctx ->
                        these |> List.map (fun authorize -> authorize ctx)
                        |> System.Threading.Tasks.Task.WhenAll
                        |> Task.map List.ofArray
                        |> Task.map Ok

            if auths |> List.contains (Ok()) then
                return ()
            else
                return!
                    auths
                    |> List.collect (function
                        | Ok() -> []
                        | Error errors -> errors)
                    |> Error
                    |> Task.wrap
        }


    let private isAuthorized permissionKey permission =
        result {
            let! user = fun ctx -> ctx.User |> Ok |> Task.wrap
            if user.HasClaim(permissionKey, permission) then
                return ()
            else
                return! [ AccessDenied $"Missing permission <{permission}> in token at '{permissionKey}'" ] |> Error |> Task.wrap
        }

    let isAuthenticated =
        result {
            let! user = fun ctx -> ctx.User |> Ok |> Task.wrap
            if user.Identity.IsAuthenticated then
                return ()
            else
                return! [ NotLoggedIn $"User not logged in" ] |> Error |> Task.wrap
        }

    let isAdmin =
        result {
            do! isAuthenticated
            let! config = fun ctx -> ctx.GetService<AppConfig>() |> Ok |> Task.wrap

            let! isAdmin = isAuthorized config.permissionsAndClaimsKey config.adminPermissionClaim
            return isAdmin
        }
    

    let getUserId: Handler<int option> =
        result {
            let! ctx = fun ctx -> ctx |> Ok |> Task.wrap
            let! res = isAuthenticated 
                            >> Task.map (function
                                                | Error _ -> None |> Ok 
                                                | Ok _ -> ctx.User.FindFirst(employeeIdClaim).Value |> Tools.tryParseInt |> Ok)
            return res
        }
        
    let getUserIdV2 (context: HttpContext) = 
        context.User.FindFirst(employeeIdClaim).Value |> Tools.tryParseInt
                

    let isAuthenticatedAs id =
        result {
            let! userId = getUserId >> Task.map (Result.bind (Option.withError [NotLoggedIn $"Could not retrieve UserId"]))
            if userId = id then
                return ()
            else
                return! [AccessDenied $"User not logged in with id:{id}"] |> Error |> Task.wrap
       }

    let isAdminOrAuthenticatedAsUser id = anyOf [isAdmin
                                                 isAuthenticatedAs id]