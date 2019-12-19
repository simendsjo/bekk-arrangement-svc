
namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

module UserMessage =

    type HttpError = HttpFunc -> HttpContext -> HttpFuncResult

    type UserMessage = string

    let convertUserMessageToHttpError (errors: UserMessage list): HttpError =
        RequestErrors.BAD_REQUEST errors