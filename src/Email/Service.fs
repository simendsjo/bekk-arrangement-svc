namespace ArrangementService.Email
open Giraffe
open FSharp.Data
open Microsoft.AspNetCore.Http
open Models
open Newtonsoft.Json

module Service =

    let sendMail email (context: HttpContext) =
        let sendgridConfig = context.GetService<SendgridOptions>()
        async {
            let! html = Http.AsyncRequestString
                            (sendgridConfig.SendgridUrl,
                             httpMethod = "POST",
                             headers = ["Authorization", (sprintf "Bearer: %s" sendgridConfig.ApiKey)
                                        "Content-Type", "application/json"],
                             body = TextRequest(JsonConvert.SerializeObject email))
            printfn "%d" html.Length } |> Async.Start
