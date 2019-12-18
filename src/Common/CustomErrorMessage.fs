
namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

module CustomErrorMessage =

    type HttpError = HttpFunc -> HttpContext -> HttpFuncResult

    type CustomErrorMessage = string

    let convertCustomErrorToHttpErr (errors: CustomErrorMessage list): HttpError =
        RequestErrors.BAD_REQUEST errors