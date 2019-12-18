namespace ArrangementService.Participants

open Giraffe

open ArrangementService
open Http
open ResultComputationExpression
open Repo
open Models

module Handlers =

    let registerForEvent (email, id) =
        result {
            for writeModel in getBody<WriteModel> do
            for participant in Service.registerParticipant (id, email) writeModel do
            return models.domainToView participant
        }

    let getParticipants =
        result {
            for participants in Service.getParticipants do
            return Seq.map models.domainToView participants
        }

    let getParticipantEvents email =
        result {
            for participants in Service.getParticipantEvents email do
            return Seq.map models.domainToView participants
        }

    let deleteParticipant (email, id) =
        result {
            for deleteResult in Service.deleteParticipant email id do
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
