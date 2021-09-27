namespace ArrangementService.DomainModels

open ArrangementService
open System

type Email =
    { Subject: string
      Message: string
      To: Email.EmailAddress
      CalendarInvite: string option }

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
      CloseRegistrationTime: Event.CloseRegistrationTime
      ParticipantQuestions: Event.ParticipantQuestions
      MaxParticipants: Event.MaxParticipants
      EditToken: Guid
      HasWaitingList: bool
      IsCancelled: bool
      IsExternal: bool
      IsHidden: bool
      OrganizerId: Event.EmployeeId
      Shortname: Event.Shortname }
    static member Create =
        fun id title description location organizerName organizerEmail maxParticipants (startDate, endDate) openForRegistrationTime closeRegistrationTime editToken participantQuestions hasWaitingList isCancelled isExternal isHidden organizerId shortname ->
            { Id = id
              Title = title
              Description = description
              Location = location
              OrganizerName = organizerName
              OrganizerEmail = organizerEmail
              MaxParticipants = maxParticipants
              StartDate = startDate
              EndDate = endDate
              OpenForRegistrationTime = openForRegistrationTime
              CloseRegistrationTime = closeRegistrationTime
              EditToken = editToken
              ParticipantQuestions = participantQuestions
              HasWaitingList = hasWaitingList
              IsCancelled = isCancelled
              IsExternal = isExternal
              IsHidden = isHidden
              OrganizerId = organizerId
              Shortname = shortname }

type Participant =
    { Name: Participant.Name
      Email: Email.EmailAddress
      ParticipantAnswers: Participant.ParticipantAnswers
      EventId: Event.Id
      RegistrationTime: TimeStamp
      CancellationToken: Guid
      EmployeeId: Participant.EmployeeId }
    static member Create =
        fun name email participantAnswers eventId registrationTime cancellationToken employeeId ->
            { Name = name
              Email = email
              ParticipantAnswers = participantAnswers
              EventId = eventId
              RegistrationTime = registrationTime
              CancellationToken = cancellationToken
              EmployeeId = employeeId }
