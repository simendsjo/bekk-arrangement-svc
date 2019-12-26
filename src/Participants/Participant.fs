namespace ArrangementService

open ArrangementService.Email

type Participant =
    { Email: EmailAddress
      EventId: Event.Id
      RegistrationTime: TimeStamp }
    static member Create =
      fun email eventId registrationTime ->
        { Email = email
          EventId = eventId
          RegistrationTime = registrationTime}
