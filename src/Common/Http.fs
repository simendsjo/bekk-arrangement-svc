namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open UserMessage
open Repo

module Http =

    type Handler<'t> = HttpContext -> Result<'t, UserMessage list>

    let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
        match condition context with
        | Ok() -> next context
        | Error errorMessage ->
            convertUserMessagesToHttpError errorMessage next context

    let handle (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        match endpoint context with
        | Ok result ->
            // TODO: Sjekk
            // at dette ikke trengs
            // commitTransaction context |> ignore
            json result next context
        | Error errorMessage ->
            // TODO: SAME AS ABOVE
            // rollbackTransaction context |> ignore
            convertUserMessagesToHttpError errorMessage next context

    let getBody<'WriteModel> (context: HttpContext): Result<'WriteModel, UserMessage list>
        =
        try
            Ok(context.BindJsonAsync<'WriteModel>().Result)
        with _ -> Error [ "Feilformatert writemodel" |> BadInput ]

    let queryParam param (ctx: HttpContext) =
        ctx.GetQueryStringValue param
        |> Result.mapError
            (fun _ ->
                [ BadInput(sprintf "Missing query parameter '%s'" param) ])
