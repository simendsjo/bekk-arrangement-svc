namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks

open ArrangementService
open UserMessage

module Http =

    type Handler<'t> = HttpContext -> Result<'t, UserMessage list>

    let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
        Database.createConnection context |> ignore
        match condition context with
        | Ok() -> next context
        | Error errorMessage ->
            convertUserMessagesToHttpError errorMessage next context

    let handle (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        let conn, transaction = Database.createConnection context
        match endpoint context with
        | Ok result ->
            transaction.Commit()
            conn.Close()
            json result next context
        | Error errorMessage ->
            transaction.Rollback()
            conn.Close()
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
                [ BadInput $"Missing query parameter '{param}'" ])

    let withRetry (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        let rec retry () =
            try
                handler next ctx
            with _ ->
                let config = Config.getConfig ctx
                config.currentTransaction.Rollback()
                config.currentConnection.Close()
                config.currentConnection <- null
                config.currentTransaction <- null
                retry () 
        retry ()

    let parseBody<'T> (ctx: HttpContext) =
        let body = 
            ctx.ReadBodyBufferedFromRequestAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Thoth.Json.Net.Decode.Auto.fromString<'T> body
        |> function
        | Ok x -> Ok x
        | Error _ -> Error [ BadInput $"Kunne ikke parse body: {body}" ]
