namespace ArrangementService.Participant

open ArrangementService

open Email.Models
open TimeStamp

type Participant =
    { Email: EmailAddress
      EventId: Event.Id
      RegistrationTime: TimeStamp }
    static member Create =
      fun email eventId registrationTime ->
        { Email = email
          EventId = eventId
          RegistrationTime = registrationTime}
