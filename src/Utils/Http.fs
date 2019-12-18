namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open System

module Http =

    type HttpError = HttpFunc -> HttpContext -> HttpFuncResult

    type CustomErrorMessage = string

    type Handler<'t> = HttpContext -> Result<'t, CustomErrorMessage list>

    let convertCustomErrorToHttpErr (errors: CustomErrorMessage list): HttpError =
        RequestErrors.BAD_REQUEST errors

    let handle (f: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        match f context with
        | Ok result -> json result next context
        | Error errorMessage -> (convertCustomErrorToHttpErr errorMessage) next context


    let getBody<'WriteModel> (context: HttpContext): Result<'WriteModel, CustomErrorMessage list> =
        try
            Ok(context.BindJsonAsync<'WriteModel>().Result)
        with _ -> Error [ "Feilformatert writemodel" ]

    let log x = x.ToString() |> Console.WriteLine

    let sideEffect f x ctx =
        f x ctx |> ignore
        Ok x
