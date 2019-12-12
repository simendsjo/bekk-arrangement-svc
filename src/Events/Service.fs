namespace ArrangementService.Events

open ArrangementService.Operators
open ArrangementService.Email.Models
open ArrangementService.Email.Service
open ArrangementService

open Queries
open ErrorMessages

module Service =

    let eventModels = Events.Models.models

    let eventRepo = Repo.from eventModels
    let participantsRepo = Repo.from Participants.Models.models

    let getEvents = eventRepo.read >> Seq.map eventModels.dbToDomain

    //    let getEventsForEmployee employeeId =
    //        eventRepo.read
    //        >> queryEventsForEmployee employeeId
    //        >> Seq.map models.dbToDomain

    let getEvent id =
        eventRepo.read
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map eventModels.dbToDomain

    let createEmail participants (event: Events.Models.DomainModel) =
        { Subject = event.Title
          Message = event.Description
          From = EmailAddress event.OrganizerEmail
          To = EmailAddress participants
          Cc = EmailAddress event.OrganizerEmail }

    let sendEventEmail participants event context =
        let mail = createEmail participants event
        sendMail mail context |> ignore

    let createEvent writemodel =
        eventRepo.create (fun id -> eventModels.writeToDomain id writemodel)
        >> Ok
        //>>= Http.sideEffect (sendEventEmail writemodel.Participants)

    let updateEvent id event =
        eventRepo.read
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map (eventRepo.update event)

    let deleteEvent id =
        eventRepo.read
        >> queryEventBy id
        >> withError (eventNotFound id)
        >> Result.map eventRepo.del
        >> Result.bind (fun _ -> eventSuccessfullyDeleted id)
    
    let registerParticipant (registration: Participants.Models.DomainModel) =
        participantsRepo.create (fun _ -> registration)
        >> Ok
        >>= Http.sideEffect
            (fun registration context -> 
                getEvent registration.EventId context
                |> Result.map 
                    (fun event -> sendEventEmail registration.ParticipantEmail event context))