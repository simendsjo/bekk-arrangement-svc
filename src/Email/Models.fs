namespace ArrangementService.Email

open ArrangementService
open SendgridApiModels
open ArrangementService.DomainModels

module Models =
    let emailToSendgridFormat (email: Email): SendGridFormat =
        { Personalizations = [ { To = [ { Email = email.To.Unwrap } ] } ]
          From = { Email = email.From.Unwrap }
          Subject = email.Subject
          Content =
              [ { Value = email.Message
                  Type = "text/html" } ]
          Attachments =
              email.CalendarInvite
              |> Option.map (fun calendarInvite ->
                  [ { Content =
                          calendarInvite
                          |> System.Text.Encoding.UTF8.GetBytes
                          |> System.Convert.ToBase64String
                      Type = "text/calendar; method=REQUEST"
                      Filename = "invite.ics" } ]) }
