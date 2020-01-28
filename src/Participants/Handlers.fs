namespace ArrangementService.Participant

open Giraffe

open ArrangementService
open Http
open ResultComputationExpression
open Repo
open Models
open ArrangementService.Email
open Authorization
open System.Web

module Handlers =

    let registerForEvent (eventId, email) =
        result {
            for writeModel in getBody<WriteModel> do
                let redirectUrlTemplate =
                    HttpUtility.UrlDecode writeModel.redirectUrlTemplate

                for participant in Service.registerParticipant
                                       redirectUrlTemplate
                                       (fun _ ->
                                           writeToDomain (eventId, email)
                                               writeModel) do
                    return domainToViewWithCancelInfo participant
        }

    let getParticipationsForParticipant email =
        result {
            let! emailAddress = EmailAddress.Parse email
            for participants in Service.getParticipationsForParticipant
                                    emailAddress do
                return Seq.map domainToView participants |> Seq.toList
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
                            (handle << getParticipationsForParticipant) ]
              DELETE
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun parameters ->
                            userCanCancel parameters
                            >=> (handle << deleteParticipant) parameters) ]
              POST
              >=> choose
                      [ routef "/events/%O/participants/%s"
                            (handle << registerForEvent) ] ]
