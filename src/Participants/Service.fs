namespace ArrangementService.Participants

open ArrangementService
open ArrangementService.Operators
open ArrangementService.Email.Models
open ArrangementService.Email.Service

open Queries
open ErrorMessages

module Service = 

    let models = Models.models
    let repo = Repo.from Models.models

    let createICSMessage (event: Events.Models.DomainModel) =
        let icsString = 
           sprintf
            "BEGIN:VCALENDAR
PRODID:-//Schedule a Meeting
VERSION:2.0
METHOD:REQUEST
BEGIN:VEVENT
DTSTART:%s
DTSTAMP:%s
DTEND:%s
LOCATION:%s
UID:%O
DESCRIPTION:%s
X-ALT-DESC;FMTTYPE=text/html:%s
SUMMARY:%s
ORGANIZER:MAILTO:%s
ATTENDEE;CN=\"%s\";RSVP=TRUE:mailto:%s
BEGIN:VALARM
TRIGGER:-PT15M
ACTION:DISPLAY
DESCRIPTION:Reminder
END:VALARM
END:VEVENT
END:VCALENDAR" 
                "20200101T192209Z"
                "20191213T192209Z" 
                "20200101T202209Z" 
                event.Location 
                event.Id 
                event.Description 
                event.Description 
                event.Title 
                "idabosch@gmail.com" 
                "Ida Marie" 
                "ida.bosch@bekk.no"
             //startTime stamp endTime location guid description description subject fromAddress toName toAddress
        icsString 

    let createEmail participants (event: Events.Models.DomainModel) =
        { Subject = event.Title
          Message = createICSMessage event
          From = EmailAddress event.OrganizerEmail
          To = EmailAddress participants
          Cc = EmailAddress event.OrganizerEmail
          CalendarInvite = createICSMessage event }

    let sendEventEmail participants event context =
        let mail = createEmail participants event
        sendMail mail context |> ignore

    let registerParticipant (registration: Models.DomainModel) =
        repo.create (fun _ -> registration)
        >> Ok
        >>= Http.sideEffect
            (fun registration context -> 
                Events.Service.getEvent registration.EventId context
                |> Result.map 
                    (fun event -> sendEventEmail registration.Email event context))

    let getParticipants = 
        repo.read >> Seq.map models.dbToDomain
    
    let getParticipantEvents email = 
        repo.read
        >> queryParticipantBy email
        >> Seq.map models.dbToDomain
        >> Ok


    let deleteParticipant email id = 
        repo.read
        >> queryParticipantByKey (email, id)
        >> withError (participationNotFound email id)
        >> Result.map repo.del
        >> Result.bind (fun _ -> participationSuccessfullyDeleted email id)