namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open System

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
            Console.WriteLine(ex)
            "Feilformatert writemodel"
            |> RequestErrors.BAD_REQUEST
            |> Error

    let log x = x.ToString() |> Console.WriteLine

    let tap f x =
        f x |> ignore
        x
