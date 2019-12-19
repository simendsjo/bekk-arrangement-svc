namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open UserMessage
open Repo

module Http =

    type Handler<'t> = HttpContext -> Result<'t, UserMessage list>

    let handle (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        match endpoint context with
        | Ok result -> json result next context
        | Error errorMessage ->
            rollbackTransaction context |> ignore
            convertUserMessageToHttpError errorMessage next context

    let getBody<'WriteModel> (context: HttpContext): Result<'WriteModel, UserMessage list> =
        try
            Ok(context.BindJsonAsync<'WriteModel>().Result)
        with _ -> Error [ "Feilformatert writemodel" ]
