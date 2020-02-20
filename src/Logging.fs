namespace ArrangementService

open System
open Giraffe
open Serilog
open Microsoft.AspNetCore.Http
open Giraffe.SerilogExtensions
open Serilog.Formatting.Json
open Microsoft.Extensions.Logging
open Serilog.Events

module Logging =

    let getEmployeeName (ctx: HttpContext) =
        let nameClaim = ctx.User.FindFirst "name"
        match nameClaim with
        | null -> "Fant ikke brukernavn"
        | _ -> nameClaim.Value

    let getEmployeeId (ctx: HttpContext) =
        let idClaim =
            ctx.User.FindFirst(ctx.GetService<AppConfig>().userIdClaimsKey)
        match idClaim with
        | null -> "Fant ikke bruker-id"
        | _ -> idClaim.Value

    let getConsumerName (ctx: HttpContext) =
        let hasValue, value = ctx.Request.Headers.TryGetValue("X-ConsumerName")
        if hasValue then
            value.ToString()
        else
            "Consumer name is not set. Make sure to set the X-ConsumerName header"

    let createExceptionMessage (ctx: HttpContext) =
        let logEventId =
            Guid.NewGuid().ToString().Split(Convert.ToChar("-")).[0]
        let userMessage =
            sprintf
                "Beklager det skjedde en feil! Den er logget med id %s Ta kontakt med Forvaltning om du ønsker videre oppfølging."

        let a =
            {| LogEventId = logEventId
               LogLevel = LogLevel.Error
               Name = getEmployeeName ctx
               UserId = getEmployeeId ctx
               UserMessage = userMessage logEventId
               RequestUrl = ctx.GetRequestUrl()
               RequestMethod = ctx.Request.Method
               RequestConsumerName = getConsumerName ctx
               RequestTraceId = ctx.TraceIdentifier
               StatusCode = StatusCodes.Status500InternalServerError |}

        fun (ex: Exception) ->
            {| a with
                   ExceptionType = ex.GetType()
                   ExceptionMessage = ex.Message
                   StackTrace = ex.StackTrace
                   InnerException = ex.InnerException |}

    let log (ctx: HttpContext) =
        let logger = ctx.Logger()
        fun (ex: Exception) ->
            let exceptionMessage = createExceptionMessage ctx ex
            logger.Error("{@Logmessage}", exceptionMessage)

    let errorHandler (ex: Exception): HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            log ctx ex
            let exceptionMessage = createExceptionMessage ctx ex
            json exceptionMessage next ctx

    let config =
        { SerilogConfig.defaults with
              ErrorHandler = fun ex _ -> setStatusCode 500 >=> errorHandler ex }
    let createLoggingApp webApp config = SerilogAdapter.Enable(webApp, config)

    Log.Logger <-
        LoggerConfiguration().Enrich.FromLogContext()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Warning()
            .WriteTo.Console(JsonFormatter())
            .CreateLogger()
