namespace ArrangementService.Email

open System.Text
open System.IO
open Giraffe
open FSharp.Data
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open ArrangementService
open Models
open SendgridApiModels

module Service =

    let icsString = 
       sprintf
        "BEGIN:VCALENDAR
         PRODID:-//Schedule a Meeting
         VERSION:2.0
         METHOD:REQUEST
         BEGIN:VEVENT
         DTSTART:%s
         DTSTAMP:%s
         DTEND:%s
         LOCATION:%s
         UID:%O
         DESCRIPTION:%s
         X-ALT-DESC;FMTTYPE=text/html:%s
         SUMMARY:%s
         ORGANIZER:MAILTO:%s
         ATTENDEE;CN=\"%s\";RSVP=TRUE:mailto:%s
         BEGIN:VALARM
         TRIGGER:-PT15M
         ACTION:DISPLAY
         DESCRIPTION:Reminder
         END:VALARM
         END:VEVENT
         END:VCALENDAR" 
            "2020-01-01T19:22:09.1440844Z" 
            "2019-12-13T19:22:09.1440844Z" 
            "2020-01-01T20:22:09.1440844Z" 
            "Skuret" 
            "eecee9a8-b8bb-411e-bed1-37210a69cf2b" 
            "beskrivelse" 
            "beskrivelse" 
            "emne" 
            "idabosch@gmail.com" 
            "Ida Marie" 
            "ida.bosch@bekk.no"
         //startTime stamp endTime location guid description description subject fromAddress toName toAddress

    let createFile content = 
        File.WriteAllText (@".\test.ics", content) |> ignore

    let private sendMailProd (options: SendgridOptions) (jsonBody: string) =
        let byteBody = UTF8Encoding().GetBytes(jsonBody)
        async {
            let! _ = Http.AsyncRequestString
                         (  options.SendgridUrl, 
                            httpMethod = "POST",
                            headers =
                              [ "Authorization", 
                                (sprintf "Bearer %s" options.ApiKey)
                                "Content-Type", 
                                "application/json"
                                "Content-Class",
                                "urn:content-classes:calendarmessage" ], 
                                body = BinaryUpload byteBody )
            ()
        }
        |> Async.Start

    let sendMail (email: Email) (context: HttpContext) =
        createFile icsString
        let sendgridConfig = context.GetService<SendgridOptions>()

        let mailFunction =
            if context.GetService<AppConfig>().isProd then sendMailProd sendgridConfig
            else printfn "%s%s" icsString

        let serializerSettings =
            let settings = JsonSerializerSettings()
            settings.ContractResolver <- CamelCasePropertyNamesContractResolver()
            settings

        (emailToSendgridFormat email, serializerSettings)
        |> JsonConvert.SerializeObject
        |> mailFunction
