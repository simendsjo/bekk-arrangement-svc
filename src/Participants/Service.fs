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

    let private createEmail createRedirectUrl (participant: Participant)
        (event: Event) =
        let message = inviteMessage (createRedirectUrl participant) event
        { Subject = event.Title.Unwrap
          Message = message
          From = event.OrganizerEmail
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment event participant.Email message }

    let private sendEventEmail createRedirectUrl (participant: Participant) =
        result {
            for event in Event.Service.getEvent participant.EventId do
                let mail = createEmail createRedirectUrl participant event
                yield Service.sendMail mail
        }

    let registerParticipant createRedirectUrl registration =
        result {

            for participant in repo.create registration do

                yield sendEventEmail createRedirectUrl participant

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

    let deleteParticipant (eventId, email) =
        result {
            for participant in getParticipant (eventId, email) do

                repo.del participant

                return participationSuccessfullyDeleted (eventId, email)
        }
