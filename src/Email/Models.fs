namespace ArrangementService.Email

module Models =

    type EmailAddress = EmailAddress of string
    let emailAddressToString = function | EmailAddress a -> a

    type Email =
        { Subject: string
          Message: string
          From: EmailAddress
          To: EmailAddress
          Cc: EmailAddress }

    // Sendgrid models
    
    type SendGridEmailAddress = { Email: string }

    type Personalization =
        { To: SendGridEmailAddress list
          Cc: SendGridEmailAddress list }

    type Content =
        { Type: string
          Value: string }

    type SendGridFormat =
        { Personalizations: Personalization list
          From: SendGridEmailAddress
          Subject: string
          Content: Content list }

    type SendgridOptions =
        { ApiKey: string
          SendgridUrl: string }


    let emailToSendgridFormat (email: Email) =
        { Personalizations = [ { To = [ { Email = emailAddressToString email.To } ]
                                 Cc = [ { Email = emailAddressToString email.Cc } ] } ]
          From = { Email = emailAddressToString email.From}
          Subject = email.Subject
          Content = [ { Value = email.Message; Type = "text/html" } ] }
