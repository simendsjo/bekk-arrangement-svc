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
open System
open ArrangementService.DomainModels

module Handlers =

    let registerForEvent (eventId, email) =
        result {
            for writeModel in getBody<WriteModel> do
                let redirectUrlTemplate =
                    HttpUtility.UrlDecode writeModel.redirectUrlTemplate

                let createRedirectUrl (participant: Participant) =
                    redirectUrlTemplate.Replace("{eventId}",
                                                participant.EventId.Unwrap.ToString
                                                    ())
                                       .Replace("{email}",
                                                participant.Email.Unwrap)
                                       .Replace("{cancellationToken}",
                                                participant.CancellationToken.ToString
                                                    ())

                for participant in Service.registerParticipant
                                       createRedirectUrl
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

    let getParticipantsForEvent id =
        result {
            for participants in Service.getParticipantsForEvent (Event.Id id) do
                return Seq.map domainToView participants |> Seq.toList
        }

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ routef "/events/%O/participants"
                            (handle << getParticipantsForEvent)
                        routef "/participants/%s/events"
                            (handle << getParticipationsForParticipant) ]
              DELETE
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun parameters ->
                            check (userCanCancel parameters)
                            >=> (handle << deleteParticipant) parameters) ]
              POST
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun (eventId: Guid, email) ->
                            check (eventHasAvailableSpots eventId)
                            >=> (handle << registerForEvent) (eventId, email)) ] ]
