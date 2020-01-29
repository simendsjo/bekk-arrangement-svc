namespace ArrangementService.Email

open System.Text
open Giraffe
open FSharp.Data
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open ArrangementService
open SendgridApiModels
open Models
open ArrangementService.DomainModels

module Service =

    let private sendMailProd (options: SendgridOptions) (jsonBody: string) =
        let byteBody = UTF8Encoding().GetBytes(jsonBody)
        async {
            let! _ = Http.AsyncRequestString
                         (options.SendgridUrl, httpMethod = "POST",
                          headers =
                              [ "Authorization",
                                (sprintf "Bearer %s" options.ApiKey)
                                "Content-Type", "application/json" ],
                          body = BinaryUpload byteBody)
            ()
        }
        |> Async.Start

    let sendMail (email: Email) (context: HttpContext) =
        let sendgridConfig = context.GetService<SendgridOptions>()
        let appConfig = context.GetService<AppConfig>()

        let serializerSettings =
            let settings = JsonSerializerSettings()
            settings.ContractResolver <-
                CamelCasePropertyNamesContractResolver()
            settings

        let serializedEmail =
            (emailToSendgridFormat email, serializerSettings)
            |> JsonConvert.SerializeObject

        let actuallySendMail() =
            serializedEmail |> sendMailProd sendgridConfig

        if appConfig.isProd then
            actuallySendMail()
        else
            printf "%A" serializedEmail
            if appConfig.sendMailInDevEnvWhiteList
               |> List.contains email.To.Unwrap then actuallySendMail()
