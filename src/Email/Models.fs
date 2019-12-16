namespace ArrangementService.Email

open SendgridApiModels

module Models =

    type EmailAddress = EmailAddress of string

    let emailAddressToString = function
        | EmailAddress a -> a

    type Email =
        { Subject: string
          Message: string
          From: EmailAddress
          To: EmailAddress
          Cc: EmailAddress }

    let emailToSendgridFormat (email: Email) attachments: SendGridFormat =
        { Personalizations =
              [ { To = [ { Email = emailAddressToString email.To } ]
                  Cc = [ { Email = emailAddressToString email.Cc } ] } ]
          From = { Email = emailAddressToString email.From }
          Subject = email.Subject
          Content =
              [ { Value = email.Message
                  Type = "text/html" }
                { Value = attachments
                  Type = "text/calendar"} ] }
          //Attachments = attachments }
