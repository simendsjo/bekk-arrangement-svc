namespace ArrangementService.DomainModels

open System.Data
open ArrangementService
open System
open ArrangementService.Participant
open Donald

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
      Shortname: Event.Shortname 
      CustomHexColor: Event.CustomHexColor }
    static member FromReader (rd: IDataReader) =
        {
            Id = rd.ReadGuid "Id" |> Event.Id
            Title = rd.ReadString "Title" |> Event.Title
            Description = rd.ReadString "Description"  |> Event.Description
            Location = rd.ReadString "Location" |> Event.Location
            StartDate =
//                let startDate = rd.ReadString "StartDate"
//                let startTime = rd.ReadString "StartTime"
                { Date = { Day = 0; Month = 0; Year = 0 }
                  Time = { Hour = 0; Minute = 0 }}
            EndDate =
//                let endDate = rd.ReadString "EndDate"
//                let endTime = rd.ReadString "EndTime"
                { Date = { Day = 0; Month = 0; Year = 0 }
                  Time = { Hour = 0; Minute = 0 }}
            OrganizerName = rd.ReadString "OrganizerName" |> Event.OrganizerName
            OrganizerEmail = rd.ReadString "OrganizerEmail" |> Email.EmailAddress
            OpenForRegistrationTime = rd.ReadInt64 "OpenForRegistrationTime" |> Event.OpenForRegistrationTime
            CloseRegistrationTime = rd.ReadInt64Option "CloseRegistrationTime" |> Event.CloseRegistrationTime
            ParticipantQuestions = [] |> Event.ParticipantQuestions
            MaxParticipants = rd.ReadInt32Option "MaxParticipants" |> Event.MaxParticipants
            EditToken = rd.ReadGuid "EditToken"
            HasWaitingList = rd.ReadBoolean "HasWaitingList"
            IsCancelled = rd.ReadBoolean "IsCancelled" 
            IsExternal = rd.ReadBoolean "IsExternal" 
            IsHidden = rd.ReadBoolean "IsHidden" 
            OrganizerId = rd.ReadInt32 "OrganizerId" |> Event.EmployeeId
            Shortname = None |> Event.Shortname
            CustomHexColor = rd.ReadStringOption "CustomHexColor" |> Event.CustomHexColor
        }
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
    { Name: Participant.Name
      Email: Email.EmailAddress
      ParticipantAnswers: Participant.ParticipantAnswers
      EventId: Event.Id
      RegistrationTime: TimeStamp
      CancellationToken: Guid
      EmployeeId: Participant.EmployeeId }
    static member FromReader (rd: IDataReader) =
        {
            Name = rd.ReadString "Name" |> Participant.Name
            Email = rd.ReadString "Email" |> Email.EmailAddress
            ParticipantAnswers = [] |> Participant.ParticipantAnswers
            EventId = rd.ReadGuid "EventId" |> Event.Id
            RegistrationTime = rd.ReadInt64 "RegistrationTime" |> TimeStamp
            CancellationToken = rd.ReadGuid "CancellationToken"
            EmployeeId = rd.ReadInt32Option "EmployeeId" |> Participant.EmployeeId
        }
    static member Create =
        fun name email participantAnswers eventId registrationTime cancellationToken employeeId ->
            { Name = name
              Email = email
              ParticipantAnswers = participantAnswers
              EventId = eventId
              RegistrationTime = registrationTime
              CancellationToken = cancellationToken
              EmployeeId = employeeId }
