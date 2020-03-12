namespace ArrangementService.Participant

open ArrangementService

open ResultComputationExpression
open ArrangementService.Email
open CalendarInvite
open Queries
open UserMessages
open Models
open ArrangementService.DomainModels
open DateTime

module Service =

    let repo = Repo.from models

    let private inviteMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Meld deg av her: %s." redirectUrl
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

    let private waitlistedMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du er nå på venteliste for %s på %s den %s."
              event.Title.Unwrap event.Location.Unwrap
              (toReadableString event.StartDate)
          "Du vil få beskjed på e-post om du rykker opp fra ventelisten."
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Meld deg av her: %s." redirectUrl
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "<br>"

    let createNewParticipantMail
        createCancelUrl
        (event: Event)
        isWaitlisted
        (participant: Participant)
        =
        let message =
            if isWaitlisted
            then waitlistedMessage (createCancelUrl participant) event
            else inviteMessage (createCancelUrl participant) event
        { Subject = event.Title.Unwrap
          Message = message
          From = event.OrganizerEmail
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant.Email, message, Create) }

    let private createCancelledEventMail
        (message: string)
        (event: Event)
        (participant: Participant)
        =
        { Subject = sprintf "Avlyst: %s" event.Title.Unwrap
          Message = message.Replace("\n", "<br>")
          From = event.OrganizerEmail
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant.Email, message, Cancel) }

    let registerParticipant createMail registration =
        result {

            for participant in repo.create registration do

                yield Service.sendMail (createMail participant)
                return participant
        }

    let getParticipant (eventId, email) =
        result {
            for participants in repo.read do

                let! participant = participants
                                   |> queryParticipantByKey (eventId, email)

                return participant
        }

    let getParticipantsForEvent (eventId: Event.Id) =
        result {
            for participants in repo.read do

                let attendees = participants |> queryParticipantsBy eventId

                return Seq.map models.dbToDomain attendees
        }

    let getParticipationsForParticipant email =
        result {
            for participants in repo.read do
                let participantsByMail =
                    participants |> queryParticipantionByParticipant email

                return Seq.map models.dbToDomain participantsByMail
        }

    let deleteParticipant (eventId, email, event) =
        result {
            for participants in getParticipantsForEvent eventId do
                (if event.hasWaitlist then
                    let participantToNotify =
                        participants
                        |> Seq.filter
                            (fun (i, participant) -> i > maxParticipants)
                        |> Seq.tryHead
                    yield participantToNotify
                          |> function
                          | Some x -> Service.sendMail x
                          | None -> ignore
                 else
                     ())
                |> ignore

            for participant in getParticipant (eventId, email) do
                repo.del participant

                return participationSuccessfullyDeleted (eventId, email)
        }


    let sendCancellationMailToParticipants
        messageToParticipants
        participants
        event
        ctx
        =
        let sendMailToParticipant participant =
            Service.sendMail
                (createCancelledEventMail messageToParticipants event
                     participant) ctx
        participants |> Seq.iter sendMailToParticipant
        Ok()
