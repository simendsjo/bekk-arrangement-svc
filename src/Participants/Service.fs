namespace ArrangementService.Participants

open ArrangementService
open ArrangementService.Operators
open ArrangementService.Email.Models
open ArrangementService.Email.Service
open CalendarInvite

open Queries
open ErrorMessages

module Service =

    let repo = Repo.from Models.models
    
    let createEmail participantEmail (event: Events.DomainModel.DomainModel) =
        { Subject = event.Title |> Events.DomainModel.unwrapTitle
          Message = event.Description |> Events.DomainModel.unwrapDescription
          From = event.OrganizerEmail
          To = participantEmail
          Cc = EmailAddress "ida.bosch@bekk.no" // Burde gjøre denne optional
          CalendarInvite =
              createCalendarAttachment event.StartDate event.EndDate event.Location event.Id
                  event.Description event.Title event.OrganizerEmail participantEmail
                  participantEmail }

    let sendEventEmail (participant: Models.DomainModel) =
        result {
            for event in Events.Service.getEvent participant.EventId do
                let mail = createEmail participant.Email event
                yield sendMail mail
        }

    let registerParticipant (eventId, email) (registration: Models.WriteModel) =
        result {
            for participant in repo.create
                                   (fun _ ->
                                       Models.models.writeToDomain (eventId, email) registration) do

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


    let deleteParticipant email id =
        result {
            for participants in repo.read do
                let! participantByMail = participants
                                         |> queryParticipantByKey (email, id)
                                         |> withError [ participationNotFound email id ]

                repo.del participantByMail

                return participationSuccessfullyDeleted email id
        }
