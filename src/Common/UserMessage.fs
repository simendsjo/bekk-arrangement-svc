
namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

module UserMessage =

    type HttpError = HttpFunc -> HttpContext -> HttpFuncResult

    type UserMessage =
        | NotFound of string
        | BadInput of string

    let convertUserMessagesToHttpError (errors: UserMessage list): HttpError =
        RequestErrors.BAD_REQUEST errors