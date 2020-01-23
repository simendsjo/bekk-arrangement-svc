namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

module UserMessage =

    type HttpError = HttpFunc -> HttpContext -> HttpFuncResult

    type UserMessage =
        | NotFound of string
        | BadInput of string

    type ErrorCodes =
        | ResourceNotFound
        | BadRequest
        | InternalError


    let convertUserMessagesToHttpError (errors: UserMessage list): HttpError =

        let reduceErrors (errorCode, errorMessages) =
            function
            | NotFound errorMessage ->
                (ResourceNotFound, errorMessages @ [ errorMessage ])
            | BadInput errorMessage ->
                let messages = errorMessages @ [ errorMessage ]
                match errorCode with
                | ResourceNotFound -> (ResourceNotFound, messages)
                | _ -> (BadRequest, messages)

        errors
        |> List.fold reduceErrors (InternalError, [])
        |> fun (errorCode, errorMessages) ->
            match errorCode with
            | ResourceNotFound -> RequestErrors.NOT_FOUND errorMessages
            | BadRequest -> RequestErrors.BAD_REQUEST errorMessages
            | InternalError ->
                ServerErrors.INTERNAL_ERROR "Something has gone wrong"
