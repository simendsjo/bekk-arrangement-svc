namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers
open System.Collections.Generic

open ArrangementService
open UserMessage
open System
open System.Threading
open System.Collections.Concurrent

module Http =

    type Handler<'t> = HttpContext -> Result<'t, UserMessage list>

    let check (condition: Handler<Unit>) (next: HttpFunc) (context: HttpContext) =
        match condition context with
        | Ok () -> next context
        | Error errorMessage ->
            convertUserMessagesToHttpError errorMessage next context

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

    let csvhandle filename (endpoint: Handler<string>) = setCsvHeaders filename >=> generalHandle setBodyFromString endpoint 
    let handle (endpoint: Handler<'t>) = generalHandle json endpoint

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

    let withTransaction (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        Database.createConnection ctx |> ignore
        try
            handler next ctx
        with e ->
            printfn "%A" e
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

        retry 50.0 10 // retry 10 times with a inital delay seed 150ms

    let readFirstFromQueue (q: ConcurrentQueue<Guid>) =
        let mutable result = Guid.Empty
        if q.TryPeek(&result) then
            Some result
        else
            None

    let withLock (lock: ConcurrentQueue<Guid>) (handler: HttpHandler) (next: HttpFunc) (ctx: HttpContext): HttpFuncResult =
        let key = Guid.NewGuid()
        lock.Enqueue(key)

        while readFirstFromQueue lock <> Some key do
            Thread.Sleep 10

        let res = handler next ctx

        let (_, _) = lock.TryDequeue()

        res

    let parseBody<'T> (ctx: HttpContext) =
        let body = 
            ctx.ReadBodyBufferedFromRequestAsync()
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Thoth.Json.Net.Decode.Auto.fromString<'T> body
        |> function
        | Ok x -> Ok x
        | Error _ -> Error [ BadInput $"Kunne ikke parse body: {body}" ]
