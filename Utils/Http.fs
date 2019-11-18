namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open ArrangementService.Database

module Http =

    type HttpErr = HttpFunc -> HttpContext -> HttpFuncResult

    type Handler<'t> = HttpContext -> Result<'t, HttpErr>

    let handle (f: Handler<'t>) (next: HttpFunc) (ctx: HttpContext) =
        match f ctx with
        | Ok result -> json result next ctx
        | Error errorMessage -> errorMessage next ctx

    let getBody<'WriteModel> (ctx: HttpContext) =
        try
            Ok(ctx.BindJsonAsync<'WriteModel>().Result)
        with ex ->
            "Feilformatert writemodel"
            |> RequestErrors.BAD_REQUEST
            |> Error

    let save (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().SubmitUpdates()

    let commitTransaction x ctx =
        save ctx
        Ok x
