namespace ArrangementService.Participants

open ArrangementService
open ArrangementService.Operators
open ArrangementService.Email.Models
open ArrangementService.Email.Service

open Queries
open ErrorMessages

module Service = 

    let repo = Repo.from Models.models
    
    let createEmail participants (event: Events.Models.DomainModel) =
        { Subject = event.Title |> Events.Models.unwrapTitle
          Message = event.Description |> Events.Models.unwrapDescription
          From = event.OrganizerEmail
          To = participants
          Cc = event.OrganizerEmail }

    let sendEventEmail (participant: Models.DomainModel) =
        result {
            for event in Events.Service.getEvent participant.EventId do
            let mail = createEmail participant.Email event
            yield sendMail mail
        }

    let registerParticipant (eventId, email) (registration: Models.WriteModel) =
        result {
            for participant
                in repo.create
                    (fun _ -> Models.models.writeToDomain (eventId, email) registration) do

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
            let participantsByMail =
                participants
                |> queryParticipantBy email
            return Seq.map Models.models.dbToDomain participantsByMail
        }


    let deleteParticipant email id = 
        result {
            for participants in repo.read do
            let! participantByMail =
                participants
                |> queryParticipantByKey (email, id)
                |> withError [participationNotFound email id]

            repo.del participantByMail

            return participationSuccessfullyDeleted email id
        }