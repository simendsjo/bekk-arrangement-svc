namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService
open CustomErrorMessage
open Repo

module Http =

    type Handler<'t> = HttpContext -> Result<'t, CustomErrorMessage list>

    let handle (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        match endpoint context with
        | Ok result -> json result next context
        | Error errorMessage ->
            rollbackTransaction context |> ignore
            convertCustomErrorToHttpErr errorMessage next context

    let getBody<'WriteModel> (context: HttpContext): Result<'WriteModel, CustomErrorMessage list> =
        try
            Ok(context.BindJsonAsync<'WriteModel>().Result)
        with _ -> Error [ "Feilformatert writemodel" ]
