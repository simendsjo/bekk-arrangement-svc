namespace ArrangementService.Participants

open ArrangementService

open ResultComputationExpression
open Email.Models
open Email.Service
open CalendarInvite
open DomainModel
open Queries
open ErrorMessages

module Service =

    let repo = Repo.from Models.models

    let createEmail participantEmail (event: Events.DomainModel.DomainModel) =
        { Subject = event.Title.Unwrap
          Message = event.Description.Unwrap
          From = event.OrganizerEmail
          To = participantEmail
          Cc = EmailAddress "ida.bosch@bekk.no" // Burde gjøre denne optional
          CalendarInvite =
              createCalendarAttachment event.StartDate event.EndDate event.Location event.Id
                  event.Description event.Title event.OrganizerEmail participantEmail
                  participantEmail }

    let sendEventEmail (participant: DomainModel) =
        result {
            for event in Events.Service.getEvent participant.EventId do
            let mail = createEmail participant.Email event
            yield sendMail mail
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

            let! participantByMail =
                participants
                |> queryParticipantByKey (eventId, email)

            repo.del participantByMail

            return participationSuccessfullyDeleted (eventId, email)
        }
