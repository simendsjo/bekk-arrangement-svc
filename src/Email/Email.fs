namespace ArrangementService

type Email =
    { Subject: string
      Message: string
      From: Email.EmailAddress
      To: Email.EmailAddress
      Cc: Email.EmailAddress
      CalendarInvite: string }