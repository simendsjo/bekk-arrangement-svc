namespace ArrangementService.Email

open ArrangementService

open Validation
open SendgridApiModels

module Models =

    let isAtSign =
        function
        | '@' -> true
        | _ -> false

    type EmailAddress =
        | EmailAddress of string
        member this.Unwrap =
            match this with
            | EmailAddress e -> e
        static member Parse(address: string) =
            [ validate (String.exists isAtSign) "Email address must include an at sign (@)" ]
            |> validateAll EmailAddress address

    type Email =
        { Subject: string
          Message: string
          From: EmailAddress
          To: EmailAddress
          Cc: EmailAddress
          CalendarInvite: string }

    let emailToSendgridFormat (email: Email): SendGridFormat =
        { Personalizations =
              [ { To = [ { Email = email.To.Unwrap } ]
                  Cc = [ { Email = email.Cc.Unwrap } ] } ]
          From = { Email = email.From.Unwrap }
          Subject = email.Subject
          Content =
              [ { Value = email.Message
                  Type = "text/html" } ]
          Attachments =
              [ { Content =
                      email.CalendarInvite
                      |> System.Text.Encoding.UTF8.GetBytes
                      |> System.Convert.ToBase64String
                  Type = "text/calendar; method=REQUEST"
                  Filename = sprintf "%s.ics" email.Subject } ] }
