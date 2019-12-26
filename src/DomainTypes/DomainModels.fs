namespace ArrangementService.DomainModels

open ArrangementService

type Email =
    { Subject: string
      Message: string
      From: Email.EmailAddress
      To: Email.EmailAddress
      Cc: Email.EmailAddress
      CalendarInvite: string }

type Event =
    { Id: Event.Id
      Title: Event.Title
      Description: Event.Description
      Location: Event.Location
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      OrganizerEmail: Email.EmailAddress
      OpenForRegistrationDate: DateTimeCustom }
      static member Create =
        fun id title description location organizerEmail (openForRegistrationDate, startDate, endDate) ->
            { Id = id
              Title = title
              Description = description
              Location = location
              OrganizerEmail = organizerEmail
              StartDate = startDate
              EndDate = endDate
              OpenForRegistrationDate = openForRegistrationDate }

type Participant =
    { Email: Email.EmailAddress
      EventId: Event.Id
      RegistrationTime: TimeStamp }
    static member Create =
      fun email eventId registrationTime ->
        { Email = email
          EventId = eventId
          RegistrationTime = registrationTime}
