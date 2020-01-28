namespace ArrangementService.Participant

open ArrangementService

open ResultComputationExpression
open CalendarInvite
open Queries
open UserMessages
open Models
open ArrangementService.DomainModels
open ArrangementService.Email

module Service =

    let repo = Repo.from models

    let createEmail redirectUrl (participant: Participant) (event: Event) =
        { Subject = sprintf "Du ble påmeldt %s" event.Title.Unwrap
          Message = createMessage redirectUrl event participant
          From = event.OrganizerEmail
          To = participant.Email
          Cc = EmailAddress "ida.bosch@bekk.no" // Burde gjøre denne optional
          CalendarInvite = createCalendarAttachment event participant }

    let sendEventEmail redirectUrl (participant: Participant) =
        result {
            for event in Event.Service.getEvent participant.EventId do
                let mail = createEmail redirectUrl participant event
                yield Service.sendMail mail
        }

    let registerParticipant redirectUrlTemplate registration =
        result {

            for participant in repo.create registration do

                let redirectUrl =
                    createRedirectUrl redirectUrlTemplate participant
                yield sendEventEmail redirectUrl participant

                return participant
        }

    let getParticipant (eventId, email) =
        result {
            for participants in repo.read do

                let! participant = participants
                                   |> queryParticipantByKey (eventId, email)

                return participant
        }

    let getParticipationsForParticipant email =
        result {
            for participants in repo.read do
                let participantsByMail =
                    participants |> queryParticipantBy email
                return Seq.map models.dbToDomain participantsByMail
        }

    let deleteParticipant (eventId, email) =
        result {
            for participant in getParticipant (eventId, email) do

                repo.del participant

                return participationSuccessfullyDeleted (eventId, email)
        }
