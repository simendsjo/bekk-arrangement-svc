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
        result {
            // let! () = fun ctx -> Ok () |> Task.wrap
            // let timer = new Diagnostics.Stopwatch()
            // timer.Start()
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
            let! participants = Service.getNumberOfParticipantsForEvent event.Id
            let isWaitlisted =
                event.HasWaitingList
                && event.MaxParticipants.Unwrap.IsSome
                && participants.Unwrap
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
        result {
            let! emailAddress = EmailAddress.Parse email |> Task.wrap |> ignoreContext
            let! participants = Service.getParticipationsForParticipant emailAddress
            return Seq.map domainToView participants |> Seq.toList
        }

    let deleteParticipant (id, email) =
        result {
            let! emailAddress = EmailAddress.Parse email |> Task.wrap |> ignoreContext
            
            let! event = Service.getEvent (Event.Id id)
            let! deleteResult = Service.deleteParticipant (event, emailAddress) 
            
            return deleteResult
        }

    let getParticipantsForEvent id =
        result {
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
        result {
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
                            check isAuthenticated
                            >=> (handle << getParticipantsForEvent) eventId
                            |> withTransaction)
                        routef "/events/%O/participants/count" (fun eventId -> 
                            check (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handle << getNumberOfParticipantsForEvent) eventId
                            |> withTransaction)
                        routef "/events/%O/participants/export" (fun eventId -> 
                            check (userCanEditEvent eventId)
                            >=> (csvHandle eventId << exportParticipationsDataForEvent) eventId
                            |> withTransaction)
                        routef "/events/%O/participants/%s/waitinglist-spot" (fun (eventId, email) ->
                            check (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handle << getWaitinglistSpot) (eventId, email)
                            |> withTransaction)
                        routef "/participants/%s/events" (fun email ->
                            check isAuthenticated
                            >=> (handle << getParticipationsForParticipant) email
                            |> withTransaction) ]
              DELETE
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun parameters ->
                            check (userCanCancel parameters)
                            >=> (handle << deleteParticipant) parameters)
                            |> withTransaction ]
              POST
              >=> choose
                      [ routef "/events/%O/participants/%s" (fun (eventId: Guid, email) ->
                            (check (oneCanParticipateOnEvent eventId)
                            >=> (handle << registerForEvent) (eventId, email))
                            |> withRetry
                            |> withLock registrationLock
                            ) ] ]


        // let timer = new Diagnostics.Stopwatch()
        // timer.Start()

        // printfn "Elapsed SELECT Time: %i" timer.ElapsedMilliseconds
