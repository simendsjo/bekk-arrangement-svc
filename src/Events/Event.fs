namespace ArrangementService

open ArrangementService.Email

type Event =
    { Id: Event.Id
      Title: Event.Title
      Description: Event.Description
      Location: Event.Location
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      OrganizerEmail: EmailAddress
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