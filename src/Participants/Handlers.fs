namespace ArrangementService.Participant

open Giraffe

open ArrangementService
open Http
open ResultComputationExpression
open Repo
open Models
open ArrangementService.Email
open Authorization

module Handlers =

    let registerForEvent (eventId, email) =
        result {
            for writeModel in getBody<WriteModel> do
                for participant in Service.registerParticipant
                                       (fun _ ->
                                           models.writeToDomain
                                               (eventId, email) writeModel) do
                    return models.domainToView participant
        }

    let getParticipantEvents email =
        result {
            let! emailAddress = EmailAddress.Parse email
            for participants in Service.getParticipantEvents emailAddress do
                return Seq.map models.domainToView participants
        }

    let deleteParticipant (id, email) =
        result {
            let! emailAddress = EmailAddress.Parse email
            for deleteResult in Service.deleteParticipant
                                    (Event.Id id, emailAddress) do
                yield commitTransaction
                return deleteResult
        }

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ routef "/participants/%s/events"
                            (handle << getParticipantEvents) ]
              DELETE
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun parameters ->
                            userCanCancel parameters
                            >=> (handle << deleteParticipant) parameters) ]
              POST
              >=> choose
                      [ routef "/events/%O/participants/%s"
                            (handle << registerForEvent) ] ]
