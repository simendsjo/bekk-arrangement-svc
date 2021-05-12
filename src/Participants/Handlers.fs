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

module Handlers =

    let registerForEvent (eventId, email) =
        result {
            let! writeModel = getBody<WriteModel>
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

            let! event = Event.Service.getEvent (Event.Id eventId)
            let createMailForParticipant =
                Service.createNewParticipantMail createCancelUrl event

            let! participant = Service.registerParticipant
                                   createMailForParticipant
                                   (fun _ ->
                                       writeToDomain (eventId, email)
                                           writeModel)
            return domainToViewWithCancelInfo participant
        }

    let getParticipationsForParticipant email =
        result {
            let! emailAddress = EmailAddress.Parse email |> ignoreContext
            let! participants = Service.getParticipationsForParticipant emailAddress
            return Seq.map domainToView participants |> Seq.toList
        }

    let deleteParticipant (id, email) =
        result {
            let! emailAddress = EmailAddress.Parse email |> ignoreContext
            
            let! event = Event.Service.getEvent (Event.Id id)
            let! deleteResult = Service.deleteParticipant (event, emailAddress)
            
            return deleteResult
        }

    let getParticipantsForEvent id =
        result {
            let! participants = Service.getParticipantsForEvent (Event.Id id)
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
                            >=> check
                                    (Event.Authorization.eventHasNotPassed
                                        eventId)
                            >=> check
                                    (Event.Authorization.eventHasOpenedForRegistration
                                        eventId)
                            >=> (handle << registerForEvent) (eventId, email)) ] ]
