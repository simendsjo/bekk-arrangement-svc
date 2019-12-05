namespace ArrangementService.Email
open System.Text
open Giraffe
open FSharp.Data
open Microsoft.AspNetCore.Http
open Models
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

module Service =

    let sendMail (email: Email) (context: HttpContext) =
        let sendgridConfig = context.GetService<SendgridOptions>()
        let serializerSettings =
            let a = JsonSerializerSettings()
            a.ContractResolver <- CamelCasePropertyNamesContractResolver()
            a
            
        let jsonBody = JsonConvert.SerializeObject ((emailToSendgridFormat email), serializerSettings)
        let byteBody = UTF8Encoding().GetBytes(jsonBody)
        
        async {
            printfn "%A" sendgridConfig.ApiKey
            let! x = Http.AsyncRequestString
                            (sendgridConfig.SendgridUrl,
                             httpMethod = "POST",
                             headers = ["Authorization", (sprintf "Bearer %s" sendgridConfig.ApiKey)
                                        "Content-Type", "application/json"],
                             body = BinaryUpload byteBody )
            ()
            } |> Async.Start
