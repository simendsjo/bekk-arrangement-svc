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
open ArrangementService.Config
open FifteenBelow.Json
open System.Collections.Generic
open System.Web

module Service =

    let private sendMailProd (options: SendgridOptions) (jsonBody: string) =
        let byteBody = UTF8Encoding().GetBytes(jsonBody)
        async {
            try
                let! _ = Http.AsyncRequestString
                             (options.SendgridUrl, httpMethod = "POST",
                              headers =
                                  [ "Authorization",
                                    $"Bearer {options.ApiKey}"
                                    "Content-Type", "application/json" ],
                              body = BinaryUpload byteBody)
                ()
            with e ->
                printfn "%A" e
                ()
        }
        |> Async.Start
        
    let sendMail (email: Email) (context: HttpContext) =

        let sendgridConfig = context.GetService<SendgridOptions>()
        let appConfig = getConfig context

        let serializerSettings =
            let converters =
                [ OptionConverter() :> JsonConverter
                  TupleConverter() :> JsonConverter
                  ListConverter() :> JsonConverter
                  MapConverter() :> JsonConverter
                  BoxedMapConverter() :> JsonConverter
                  UnionConverter() :> JsonConverter ]
                |> List.toArray :> IList<JsonConverter>

            let settings = JsonSerializerSettings()
            settings.ContractResolver <-
                CamelCasePropertyNamesContractResolver()
            settings.NullValueHandling <- NullValueHandling.Ignore
            settings.Converters <- converters
            settings

        let serializedEmail =
            (emailToSendgridFormat email (EmailAddress appConfig.noReplyEmail),
             serializerSettings) |> JsonConvert.SerializeObject

        let actuallySendMail() =
            serializedEmail |> sendMailProd sendgridConfig

        if appConfig.isProd then
            actuallySendMail()
        else
            printfn "%A" serializedEmail
            if appConfig.sendMailInDevEnvWhiteList
               |> List.contains email.To.Unwrap then actuallySendMail()
