namespace ArrangementService

module SendgridApiModels =

    type SendGridEmailAddress =
        { Email: string }

    type Personalization =
        { To: SendGridEmailAddress list
          Cc: SendGridEmailAddress list }

    type Content =
        { Type: string
          Value: string }

    type Attachment =
        { Content: string
          Type: string
          Filename: string }

    type SendGridFormat =
        { Personalizations: Personalization list
          From: SendGridEmailAddress
          Subject: string
          Content: Content list
          Attachments: Attachment list }

    type SendgridOptions =
        { ApiKey: string
          SendgridUrl: string }
