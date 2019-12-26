namespace ArrangementService.Participant

open Giraffe

open ArrangementService
open Http
open ResultComputationExpression
open Repo
open Models
open ArrangementService.Email

module Handlers =

    let registerForEvent (email, eventId) =
        result {
            for writeModel in getBody<WriteModel> do
            for participant in Service.registerParticipant
                (fun _ -> models.writeToDomain (eventId, email) writeModel) do
            return models.domainToView participant
        }

    let getParticipants =
        result {
            for participants in Service.getParticipants do
            return Seq.map models.domainToView participants
        }

    let getParticipantEvents email =
        result {
            let! emailAddress = EmailAddress.Parse email
            for participants in Service.getParticipantEvents emailAddress do
            return Seq.map models.domainToView participants
        }

    let deleteParticipant (email, id) =
        result {
            let! emailAddress = EmailAddress.Parse email
            for deleteResult in Service.deleteParticipant (Event.Id id, emailAddress) do
            yield commitTransaction
            return deleteResult
        }

    let routes: HttpHandler =
        choose
            [ GET >=> choose
                          [ route "/participants" >=> handle getParticipants
                            routef "/participant/%s" (handle << getParticipantEvents) ]
              DELETE >=> choose [ routef "/participant/%s/events/%O" (handle << deleteParticipant) ]
              POST >=> choose [ routef "/participant/%s/events/%O" (handle << registerForEvent) ] ]
