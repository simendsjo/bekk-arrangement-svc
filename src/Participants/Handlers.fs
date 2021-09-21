namespace ArrangementService.Participant

open Giraffe

open Microsoft.Net.Http.Headers
open System.Web
open System

open ArrangementService
open Http
open ResultComputationExpression
open Models
open ArrangementService.Email
open Authorization
open ArrangementService.DomainModels
open ArrangementService.Config
open ArrangementService.Event.Authorization
open ArrangementService.Auth
open System.Threading

module Handlers =

    let registerForEvent (eventId: Guid, email) =
        taskResult {
            let! writeModel = parseBody<WriteModel>

            let redirectUrlTemplate =
                HttpUtility.UrlDecode writeModel.cancelUrlTemplate

            let createCancelUrl (participant: Participant) =
                redirectUrlTemplate.Replace("{eventId}",
                                            participant.EventId.Unwrap.ToString
                                                ())
                                   .Replace("{email}",
                                            participant.Email.Unwrap
                                            |> Uri.EscapeDataString)
                                   .Replace("{cancellationToken}",
                                            participant.CancellationToken.ToString
                                                ())

            let! event = Service.getEvent (Event.Id eventId)
            let! participants = Service.getParticipantsForEvent event
            let isWaitlisted =
                event.HasWaitingList
                && event.MaxParticipants.Unwrap.IsSome
                && participants.attendees
                   |> Seq.length
                   >= event.MaxParticipants.Unwrap.Value
            let! config = getConfig >> Ok >> Task.wrap
            let createMailForParticipant =
                Service.createNewParticipantMail
                    createCancelUrl event isWaitlisted
                    (EmailAddress config.noReplyEmail)
            
            let! userId = Auth.getUserId 

            let! participantDomainModel = writeToDomain (eventId, email) writeModel userId |> Task.wrap |> ignoreContext
            do! Service.registerParticipant createMailForParticipant participantDomainModel

            return domainToViewWithCancelInfo participantDomainModel
        }

    let getParticipationsForParticipant email =
        taskResult {
            let! emailAddress = EmailAddress.Parse email |> Task.wrap |> ignoreContext
            let! participants = Service.getParticipationsForParticipant emailAddress
            return Seq.map domainToView participants |> Seq.toList
        }

    let deleteParticipant (id, email) =
        taskResult {
            let! emailAddress = EmailAddress.Parse email |> Task.wrap |> ignoreContext
            
            let! event = Service.getEvent (Event.Id id)
            let! deleteResult = Service.deleteParticipant (event, emailAddress) 
            
            return deleteResult
        }

    let getParticipantsForEvent id =
        taskResult {
            let! event = Service.getEvent (Event.Id id)
            let! participants = Service.getParticipantsForEvent event
            let hasWaitingList = event.HasWaitingList
            let! userCanGetInformation = 
                userCanEditEvent id
                    >> Task.map (function 
                    | Ok () -> Ok true
                    | Error _ -> Ok false)

            return { attendees =
                         Seq.map domainToView participants.attendees
                         |> Seq.map (fun p -> if not userCanGetInformation then {p with Email = None; ParticipantAnswers = []} else p) 
                         |> Seq.toList
                     waitingList =
                         if hasWaitingList then
                             Seq.map domainToView
                                 participants.waitingList
                             |> Seq.map (fun p -> if not userCanGetInformation then {p with Email = None; ParticipantAnswers = []} else p) 
                             |> Seq.toList
                             |> Some
                         else
                             None }
        }

    let getNumberOfParticipantsForEvent id =
        taskResult {
            let! count = Service.getNumberOfParticipantsForEvent (Event.Id id)
            return count.Unwrap
        }

    let getWaitinglistSpot (eventId, email) = Service.getWaitinglistSpot (Event.Id eventId) (EmailAddress email) 


    let exportParticipationsDataForEvent eventId = Service.exportParticipationsDataForEvent (Event.Id eventId)

    let registrationLock = new SemaphoreSlim(1,1)

    let routes: HttpHandler =
        choose
            [ GET_HEAD
              >=> choose
                      [ routef "/events/%O/participants" (fun eventId ->
                            checkAsync isAuthenticated
                            >=> (handleAsync << getParticipantsForEvent) eventId
                            |> withTransaction)
                        routef "/events/%O/participants/count" (fun eventId -> 
                            checkAsync (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handleAsync << getNumberOfParticipantsForEvent) eventId
                            |> withTransaction)
                        routef "/events/%O/participants/export" (fun eventId -> 
                            checkAsync (userCanEditEvent eventId)
                            >=> (csvhandleAsync eventId << exportParticipationsDataForEvent) eventId
                            |> withTransaction)
                        routef "/events/%O/participants/%s/waitinglist-spot" (fun (eventId, email) ->
                            checkAsync (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handleAsync << getWaitinglistSpot) (eventId, email)
                            |> withTransaction)
                        routef "/participants/%s/events" (fun email ->
                            checkAsync isAuthenticated
                            >=> (handleAsync << getParticipationsForParticipant) email
                            |> withTransaction) ]
              DELETE
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun parameters ->
                            checkAsync (userCanCancel parameters)
                            >=> (handleAsync << deleteParticipant) parameters)
                            |> withTransaction ]
              POST
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun (eventId: Guid, email) ->
                            (checkAsync (oneCanParticipateOnEvent eventId)
                            >=> (handleAsync << registerForEvent) (eventId, email))
                            |> withRetry
                            |> withLock registrationLock
                            ) ] ]


        // let timer = new Diagnostics.Stopwatch()
        // timer.Start()

        // printfn "Elapsed SELECT Time: %i" timer.ElapsedMilliseconds
