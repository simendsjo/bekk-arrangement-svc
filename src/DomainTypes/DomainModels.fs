namespace ArrangementService.DomainModels

open ArrangementService
open System

type Email =
    { Subject: string
      Message: string
      From: Email.EmailAddress
      To: Email.EmailAddress
      CalendarInvite: string }

type Event =
    { Id: Event.Id
      Title: Event.Title
      Description: Event.Description
      Location: Event.Location
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      OrganizerName: Event.OrganizerName
      OrganizerEmail: Email.EmailAddress
      OpenForRegistrationTime: Event.OpenForRegistrationTime
      MaxParticipants: Event.MaxParticipants }
    static member Create =
        fun id title description location organizerName organizerEmail maxParticipants (startDate, endDate) openForRegistrationTime ->
            { Id = id
              Title = title
              Description = description
              Location = location
              OrganizerName = organizerName
              OrganizerEmail = organizerEmail
              MaxParticipants = maxParticipants
              StartDate = startDate
              EndDate = endDate
              OpenForRegistrationTime = openForRegistrationTime }

type Participant =
    { Email: Email.EmailAddress
      EventId: Event.Id
      RegistrationTime: TimeStamp
      CancellationToken: Guid }
    static member Create =
        fun email eventId registrationTime cancellationToken ->
            { Email = email
              EventId = eventId
              RegistrationTime = registrationTime
              CancellationToken = cancellationToken }
