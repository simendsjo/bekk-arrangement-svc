namespace ArrangementService.Participant

open Giraffe

open ArrangementService
open Http
open ResultComputationExpression
open Models
open ArrangementService.Email
open Authorization
open System.Web
open System
open ArrangementService.DomainModels
open ArrangementService.Config

module Handlers =

    let registerForEvent (eventId, email) =
        result {
            for writeModel in getBody<WriteModel> do
                let redirectUrlTemplate =
                    HttpUtility.UrlDecode writeModel.cancelUrlTemplate

                let createCancelUrl (participant: Participant) =
                    redirectUrlTemplate.Replace("{eventId}",
                                                participant.EventId.Unwrap.ToString
                                                    ())
                                       .Replace("{email}",
                                                participant.Email.Unwrap)
                                       .Replace("{cancellationToken}",
                                                participant.CancellationToken.ToString
                                                    ())

                for event in Event.Service.getEvent (Event.Id eventId) do
                    for participants in Service.getParticipantsForEvent event do
                        let isWaitlisted =
                            event.HasWaitingList
                            && participants.attendees
                               |> Seq.length
                               >= event.MaxParticipants.Unwrap

                        for config in getConfig >> Ok do
                            let createMailForParticipant =
                                Service.createNewParticipantMail
                                    createCancelUrl event isWaitlisted
                                    (EmailAddress config.noReplyEmail)

                            for participant in Service.registerParticipant
                                                   createMailForParticipant
                                                   (fun _ ->
                                                       writeToDomain
                                                           (eventId, email)
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
            for event in Event.Service.getEvent (Event.Id id) do
                for deleteResult in Service.deleteParticipant
                                        (event, emailAddress) do
                    return deleteResult
        }

    let getParticipantsForEvent id =
        result {
            for event in Event.Service.getEvent (Event.Id id) do
                for participants in Service.getParticipantsForEvent event do
                    let hasWaitingList = event.HasWaitingList
                    return {| attendees =
                                  Seq.map domainToView participants.attendees
                              waitingList =
                                  if hasWaitingList then
                                      Seq.map domainToView
                                          participants.waitingList |> Some
                                  else
                                      None |}
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
                            >=> check
                                    (Event.Authorization.eventHasNotPassed
                                        eventId)
                            >=> check
                                    (Event.Authorization.eventHasOpenedForRegistration
                                        eventId)
                            >=> (handle << registerForEvent) (eventId, email)) ] ]
