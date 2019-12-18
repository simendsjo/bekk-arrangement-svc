namespace ArrangementService.Participants

open ArrangementService

open Email.Models
open TimeStamp

module DomainModel =
    type DomainModel =
        { Email: EmailAddress
          EventId: Events.DomainModel.Id
          RegistrationTime: TimeStamp }
        static member Create =
          fun email eventId registrationTime ->
            { Email = email
              EventId = eventId
              RegistrationTime = registrationTime}
