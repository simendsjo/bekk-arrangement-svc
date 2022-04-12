module DomainModels

open System

type Email =
    { Subject: string
      Message: string
      To: Email.Types.EmailAddress
      CalendarInvite: string option }

type Event =
    { Id: Event.Types.Id
      Title: Event.Types.Title
      Description: Event.Types.Description
      Location: Event.Types.Location
      StartDate: DateTimeCustom.DateTimeCustom
      EndDate: DateTimeCustom.DateTimeCustom
      OrganizerName: Event.Types.OrganizerName
      OrganizerEmail: Email.Types.EmailAddress
      OpenForRegistrationTime: Event.Types.OpenForRegistrationTime
      CloseRegistrationTime: Event.Types.CloseRegistrationTime
      ParticipantQuestions: Event.Types.ParticipantQuestions
      MaxParticipants: Event.Types.MaxParticipants
      EditToken: Guid
      HasWaitingList: bool
      IsCancelled: bool
      IsExternal: bool
      IsHidden: bool
      OrganizerId: Event.Types.EmployeeId
      Shortname: Event.Types.Shortname 
      CustomHexColor: Event.Types.CustomHexColor }
    static member Create =
        fun id title description location organizerName organizerEmail maxParticipants (startDate, endDate) openForRegistrationTime closeRegistrationTime editToken participantQuestions hasWaitingList isCancelled isExternal isHidden organizerId shortname hexCode ->
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
              Shortname = shortname
              CustomHexColor = hexCode }

type Participant =
    { Name: Participant.Types.Name
      Email: Email.Types.EmailAddress
      ParticipantAnswers: Participant.Types.ParticipantAnswers
      EventId: Event.Types.Id
      RegistrationTime: TimeStamp.TimeStamp
      CancellationToken: Guid
      EmployeeId: Participant.Types.EmployeeId }
    static member Create =
        fun name email participantAnswers eventId registrationTime cancellationToken employeeId ->
            { Name = name
              Email = email
              ParticipantAnswers = participantAnswers
              EventId = eventId
              RegistrationTime = registrationTime
              CancellationToken = cancellationToken
              EmployeeId = employeeId }
    static member CreateFromPrimitives =
        fun name email participantAnswers eventId registrationTime cancellationToken employeeId ->
            { Name = name |> Participant.Types.Name
              Email = email |> Email.Types.EmailAddress
              ParticipantAnswers = participantAnswers |> Participant.Types.ParticipantAnswers
              EventId = eventId |> Event.Types.Id
              RegistrationTime = registrationTime |> TimeStamp.TimeStamp
              CancellationToken = cancellationToken
              EmployeeId = employeeId |> Participant.Types.EmployeeId }