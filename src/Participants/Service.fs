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
          To = participant.Email
          Cc = Email.EmailAddress "ida.bosch@bekk.no" // Burde gjøre denne optional
          CalendarInvite =
              createCalendarAttachment event.StartDate event.EndDate event.Location event.Id event.Description
                  event.Title event.OrganizerEmail participant.Email participant.Email }

    let sendEventEmail (participant: Participant) =
        result {
            for event in Event.Service.getEvent participant.EventId do
                let mail = createEmail participant event
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

                let! participantByMail = participants |> queryParticipantByKey (eventId, email)

                repo.del participantByMail

                return participationSuccessfullyDeleted (eventId, email)
        }
