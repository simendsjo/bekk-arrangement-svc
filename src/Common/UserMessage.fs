namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

module UserMessage =

    type HttpError = HttpFunc -> HttpContext -> HttpFuncResult

    type UserMessage =
        | BadInput of string
        | AccessDenied of string
        | NotFound of string
        | InternalErrorMessage of string
        | NotLoggedIn of string

    type private ErrorCodes =
        | BadRequest
        | Forbidden
        | ResourceNotFound
        | InternalError
        | Unauthorized

    type private LessThanGreaterOrSame =
        | Same of string
        | Greater of ErrorCodes * string
        | Less

    type private UserJsonMessage = { userMessage: string }

    let private errorMessagesToUserJson messages =
        { userMessage = messages |> String.concat "\n" }

    let convertUserMessagesToHttpError (log: int -> unit) (errors: UserMessage list) : HttpError =
        let reduceErrors (errorCode, errorMessages) current =

            let hasGreaterOrEqualSeverityThan prev next =
                match prev, next with
                | ResourceNotFound, NotFound m -> Same m
                | ResourceNotFound, _ -> Less

                | Forbidden, NotFound m -> Greater(ResourceNotFound, m)
                | Forbidden, AccessDenied m -> Same m
                | Forbidden, NotLoggedIn m -> Greater(Unauthorized, m)
                | Forbidden, _ -> Less

                | BadRequest, NotFound m -> Greater(ResourceNotFound, m)
                | BadRequest, AccessDenied m -> Greater(Forbidden, m)
                | BadRequest, BadInput m -> Same m

                | _, NotFound m -> Greater(ResourceNotFound, m)
                | _, AccessDenied m -> Greater(Forbidden, m)
                | _, BadInput m -> Greater(BadRequest, m)
                | _, InternalErrorMessage m -> Greater(InternalError, m)
                | _, NotLoggedIn m -> Greater(Unauthorized, m)

            match current |> hasGreaterOrEqualSeverityThan errorCode with
            | Greater (greaterErrorCode, message) -> (greaterErrorCode, [ message ])
            | Same message -> (errorCode, errorMessages @ [ message ])
            | Less -> (errorCode, errorMessages)

        errors
        |> List.fold reduceErrors (InternalError, [])
        |> fun (errorCode, errorMessages) -> (errorCode, errorMessagesToUserJson errorMessages)
        |> fun (errorCode, userMessage) ->
            match errorCode with
            | BadRequest ->
                log 400
                RequestErrors.BAD_REQUEST userMessage
            | Forbidden ->
                log 403
                RequestErrors.FORBIDDEN userMessage
            | ResourceNotFound ->
                log 404
                RequestErrors.NOT_FOUND userMessage
            | Unauthorized ->
                log 401
                RequestErrors.UNAUTHORIZED "Bearer" "Access to internal event" userMessage
            | InternalError ->
                log 500
                [ "Something has gone wrong" ]
                |> errorMessagesToUserJson
                |> ServerErrors.INTERNAL_ERROR
