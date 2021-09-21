namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers

open ArrangementService
open ArrangementService.ResultComputationExpression
open UserMessage
open System
open System.Threading
open FSharp.Control.Tasks.V2

module Http =

    type Handler<'t> = HttpContext -> Result<'t, UserMessage list>

    let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
        match condition context with
        | Ok () -> next context
        | Error errorMessage ->
            convertUserMessagesToHttpError errorMessage next context

    let checkAsync (condition: AsyncHandler<Unit>) (next: HttpFunc) (context: HttpContext) =
        task {
            let! checkResult = condition context
            return!
                match checkResult with
                | Ok () -> 
                    next context
                | Error errorMessage -> 
                    convertUserMessagesToHttpError errorMessage next context
        }


    let setCsvHeaders (filename:Guid) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            ctx.SetHttpHeader (HeaderNames.ContentType, "text/csv")
            ctx.SetHttpHeader (HeaderNames.ContentDisposition, $"attachment; filename=\"{filename}.csv\"")
            next ctx

    let generalHandle (responseBodyFunc: ('t -> HttpHandler)) (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        match endpoint context with
        | Ok result ->
            Database.commitTransaction context
            responseBodyFunc result next context
        | Error errorMessage ->
            Database.rollbackTransaction context
            convertUserMessagesToHttpError errorMessage next context

    let generalHandleAsync (responseBodyFunc: ('t -> HttpHandler)) (endpoint: AsyncHandler<'t>) (next: HttpFunc) (context: HttpContext) =
        task {
            let! res = endpoint context
            return! 
                match res with
                | Ok result ->
                    Database.commitTransaction context
                    responseBodyFunc result next context
                | Error errorMessage ->
                    Database.rollbackTransaction context
                    convertUserMessagesToHttpError errorMessage next context
        }


    let csvhandle filename (endpoint: Handler<string>) = setCsvHeaders filename >=> generalHandle setBodyFromString endpoint 
    let csvhandleAsync filename (endpoint: AsyncHandler<string>) = setCsvHeaders filename >=> generalHandleAsync setBodyFromString endpoint 

    let handle (endpoint: Handler<'t>) = generalHandle json endpoint
    let handleAsync (endpoint: AsyncHandler<'t>) = generalHandleAsync json endpoint

    let getBody<'WriteModel> (): AsyncHandler<'WriteModel> =
        fun ctx ->
            try
                Ok(ctx.BindJsonAsync<'WriteModel>().Result) |> Task.unit
            with _ ->
                Error [ "Feilformatert writemodel" |> BadInput ] |> Task.unit

    let queryParam param =
        taskResult {
            let! res =
                fun ctx ->
                    ctx.GetQueryStringValue param
                    |> Result.mapError
                        (fun _ ->
                            [ BadInput $"Missing query parameter '{param}'" ])
                    |> Task.unit
            return res
        }

    let withTransaction (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        Database.createConnection ctx |> ignore
        try
            handler next ctx
        with _ ->
            Database.rollbackTransaction ctx
            convertUserMessagesToHttpError [] next ctx // Default is 500 Internal Server Error

    let withTransactionAsync (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        Database.createConnection ctx |> ignore
        try
            handler next ctx
        with _ ->
            Database.rollbackTransaction ctx
            convertUserMessagesToHttpError [] next ctx // Default is 500 Internal Server Error

    let withRetry (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        let seed = Guid.NewGuid().GetHashCode()
        let rnd = Random(seed)

        let rec retry delay amount =
            Database.createConnection ctx |> ignore
            try
                handler next ctx
            with _ ->
                Database.rollbackTransaction ctx

                let jitter = rnd.NextDouble() * 5.0 + 1.5 // [1.5, 6.5]
                let delayWithJitter =
                    2.0 * delay * jitter + 20.0 * jitter

                Thread.Sleep (int delayWithJitter)

                if amount > 0 then
                    retry delayWithJitter (amount-1) 
                else
                    convertUserMessagesToHttpError [] next ctx // Default is 500 Internal Server Error

        retry 50.0 10 // retry 10 times with a inital delay seed 50ms

    let withLock (lock: SemaphoreSlim) (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        task {
            do! lock.WaitAsync() 

            let timer = new Diagnostics.Stopwatch()
            timer.Start()

            let! res = handler next ctx

            printfn "%Ams" timer.ElapsedMilliseconds
            lock.Release() |> ignore

            return res
        }

    let parseBody<'T> =
        taskResult {
            let! body = 
                fun ctx ->
                    ctx.ReadBodyBufferedFromRequestAsync()
                    |> Task.map Ok

            let res = Thoth.Json.Net.Decode.Auto.fromString<'T> body
            match res with
            | Ok x ->
                return x
            | Error _ ->
                return!
                    Error [ BadInput $"Kunne ikke parse body: {body}" ]
                    |> Task.unit
        }
