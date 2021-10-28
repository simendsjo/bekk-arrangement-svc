namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers

open ArrangementService
open ArrangementService.ResultComputationExpression
open UserMessage
open System
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks.V2

module Http =
    
    let private logStatusCode ctx code =
        Logging.log "Http error" [ "http_status_code", code.ToString() ] ctx |> ignore
    
    let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
        task {
            let! checkResult = condition context
            return!
                match checkResult with
                | Ok () -> 
                    next context
                | Error errorMessage ->
                    let checkFailure = "check_failure", errorMessage |> Seq.map (fun x -> x.ToString()) |> String.concat ";"
                    Logging.log "Check failed" [ checkFailure ] context |> ignore

                    Database.rollbackTransaction context
                    convertUserMessagesToHttpError (logStatusCode context) errorMessage next context
        }


    let setCsvHeaders (filename:Guid) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            ctx.SetHttpHeader (HeaderNames.ContentType, "text/csv")
            ctx.SetHttpHeader (HeaderNames.ContentDisposition, $"attachment; filename=\"{filename}.csv\"")
            next ctx

    let generalHandle (responseBodyFunc: ('t -> HttpHandler)) (endpoint: Handler<'t>) (next: HttpFunc) (context: HttpContext) =
        task {
            let method = context.Request.Method.ToString()
            let path = context.Request.Path.ToString()
            Logging.log "Request" [ "method", method; "path", path ] context |> ignore

            let! res = endpoint context
            return! 
                match res with
                | Ok result ->
                    Database.commitTransaction context
                    Logging.log "Request succeeded" [ "request_success", "true" ] context |> ignore
                    responseBodyFunc result next context
                | Error errorMessage ->
                    let handleFailure = "handle_failure", errorMessage |> Seq.map (fun x -> x.ToString()) |> String.concat ";"
                    Logging.log "Request failed" [ handleFailure; "request_success", "false" ] context |> ignore

                    Database.rollbackTransaction context
                    convertUserMessagesToHttpError (logStatusCode context) errorMessage next context
        }


    let csvHandle filename (endpoint: Handler<string>) = setCsvHeaders filename >=> generalHandle setBodyFromString endpoint 
    let handle (endpoint: Handler<'t>) = generalHandle json endpoint

    let getBody<'WriteModel> (): Handler<'WriteModel> =
        fun ctx ->
            try
                Ok(ctx.BindJsonAsync<'WriteModel>().Result) |> Task.wrap
            with _ ->
                Error [ "Feilformatert writemodel" |> BadInput ] |> Task.wrap

    let queryParam param =
        result {
            let! res =
                fun ctx ->
                    ctx.GetQueryStringValue param
                    |> Result.mapError
                        (fun _ ->
                            [ BadInput $"Missing query parameter '{param}'" ])
                    |> Task.wrap
            return res
        }

    let withTransaction (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        task {
            try
                Logging.log "Request started" ["request_started_at", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()] ctx |> ignore
                try
                    Database.createConnection ctx 
                    return! handler next ctx
                with _ ->
                    Database.rollbackTransaction ctx
                    return! convertUserMessagesToHttpError (logStatusCode ctx) [] next ctx // Default is 500 Internal Server Error
            finally
                Logging.log "Request finished" ["request_finished_at", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()] ctx |> ignore
                Logging.canonicalLog ctx
        }

    let withRetry (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        task {
            let seed = Guid.NewGuid().GetHashCode()
            let rnd = Random(seed)

            let rec retry delay amount =
                task {
                    try
                        Database.createConnection ctx 
                        return! handler next ctx
                    with _ ->
                        Logging.log "Transaction failed, retrying..."
                            [ "retry_attempts_left", amount.ToString()
                              "current_retry_delay", delay.ToString() ] ctx |> ignore
                            
                        Database.rollbackTransaction ctx

                        let jitter = rnd.NextDouble() * 5.0 + 1.5 // [1.5, 6.5]
                        let delayWithJitter =
                            2.0 * delay * jitter + 20.0 * jitter

                        do! Task.Delay (int delayWithJitter)

                        if amount > 0 then
                            return! retry delayWithJitter (amount-1) 
                        else
                            Logging.log "Retry failed"
                                [ "current_retry_delay", delay.ToString() ] ctx |> ignore
                            return! convertUserMessagesToHttpError (logStatusCode ctx) [] next ctx 
                }

            return! retry 50.0 10 // retry 10 times with a inital delay seed 50ms
        }

    let withLock (lock: SemaphoreSlim) (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        task {
            try
                Logging.log "Request started" ["request_started_at", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()] ctx |> ignore
                do! lock.WaitAsync() 

                let! res = handler next ctx

                lock.Release() |> ignore

                Logging.log "Request finished" ["request_finished_at", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()] ctx |> ignore
                return res
            finally
                Logging.canonicalLog ctx
        }

    let parseBody<'T> =
        result {
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
                    |> Task.wrap
        }
