namespace ArrangementService.Participant

open ArrangementService

open ResultComputationExpression
open CalendarInvite
open Queries
open UserMessages
open ArrangementService.DomainModels

module Service =

    let repo = Repo.from Models.models

    let createEmail participant (event: Event) =
        { Subject = sprintf "Du ble påmeldt %s" event.Title.Unwrap
          Message = (createMessage event participant)
          From = event.OrganizerEmail
          To = Email.EmailAddress participant
          Cc = Email.EmailAddress "ida.bosch@bekk.no" // Burde gjøre denne optional
          CalendarInvite = createCalendarAttachment event participant }

    let sendEventEmail (participant: Participant) =
        result {
            for event in Event.Service.getEvent participant.EventId do
                let mail = createEmail participant.Email.Unwrap event
                yield Email.Service.sendMail mail
        }

    let registerParticipant registration =
        result {
            for participant in repo.create registration do

                yield sendEventEmail participant

                return participant
        }

    let getParticipants =
        result {
            for participants in repo.read do
                return Seq.map Models.models.dbToDomain participants
        }

    let getParticipantEvents email =
        result {
            for participants in repo.read do
                let participantsByMail = participants |> queryParticipantBy email
                return Seq.map Models.models.dbToDomain participantsByMail
        }

    let deleteParticipant (eventId, email) =
        result {
            for participants in repo.read do

                let! participant = participants |> queryParticipantByKey (eventId, email)

                repo.del participant

                return participationSuccessfullyDeleted (eventId, email)
        }
