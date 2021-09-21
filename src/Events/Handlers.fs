namespace ArrangementService.Event

open ArrangementService

open Http
open ResultComputationExpression
open Models
open ArrangementService.DomainModels
open ArrangementService.Config
open ArrangementService.Email
open Authorization

open Microsoft.AspNetCore.Http
open Giraffe
open System.Web
open ArrangementService.Auth

open FSharp.Control.Tasks.V2

module Handlers =

    type RemoveEvent = 
        | Cancel
        | Delete

    let getEvents: AsyncHandler<ViewModel list> =
        taskResult {
            let! events = Service.getEvents
            return Seq.map domainToView events |> Seq.toList
        }

    let getPastEvents: AsyncHandler<ViewModel list> =
        taskResult {
            let! events = Service.getPastEvents
            return Seq.map domainToView events |> Seq.toList
        }

    let getEventsOrganizedBy organizerEmail =
        taskResult {
            let! events = Service.getEventsOrganizedBy (EmailAddress organizerEmail)
            return Seq.map domainToView events |> Seq.toList
        }

    let getEvent id =
        taskResult {
            let! event = Service.getEvent (Id id)
            return domainToView event
        }

        
    let deleteOrCancelEvent (removeEventType:RemoveEvent) id: AsyncHandler<string> =
        taskResult {
            let! messageToParticipants = getBody<string> ()
            let! event = Service.getEvent (Id id)
            let! participants = Service.getParticipantsForEvent event

            let! config = getConfig >> Ok >> Task.wrap

            let! result =  match removeEventType with 
                            | Cancel -> Service.cancelEvent event
                            | Delete -> Service.deleteEvent (Id id)
            
            yield Service.sendCancellationMailToParticipants
                      messageToParticipants (EmailAddress config.noReplyEmail) participants.attendees event

            return result
        }

    let getEmployeeId = 
        taskResult {
            let! userId = Auth.getUserId 

            return! userId
                    |> Option.map Event.EmployeeId  // option EmployeeId
                    |> Option.withError [UserMessages.couldNotRetrieveUserId] // Result<EmployeeId, UserMessage list>
                    |> Task.wrap
        }

    let updateEvent (id:Key) =
        taskResult {
            let! writeModel = getBody<WriteModel> ()
            let! updatedEvent = Service.updateEvent (Id id) writeModel
            return domainToView updatedEvent
        }

    let createEvent =
        taskResult {
            let! writeModel = getBody<WriteModel> ()

            let redirectUrlTemplate =
                HttpUtility.UrlDecode writeModel.editUrlTemplate

            let viewUrl = writeModel.viewUrl
            let createEditUrl (event: Event) =
                redirectUrlTemplate.Replace("{eventId}",
                                            event.Id.Unwrap.ToString())
                                   .Replace("{editToken}",
                                            event.EditToken.ToString())

            let! employeeId = getEmployeeId

            let! newEvent = Service.createEvent viewUrl createEditUrl employeeId.Unwrap writeModel

            return domainToViewWithEditInfo newEvent
        }

    let deleteEvent = deleteOrCancelEvent Delete
    let cancelEvent = deleteOrCancelEvent Cancel


    let getEventAndParticipationSummaryForEmployee employeeId = 
        taskResult {
            let! events = Service.getEventsOrganizedByOrganizerId (Event.EmployeeId employeeId)
            let! participations = Service.getParticipationsByEmployeeId (Event.EmployeeId employeeId)
            return Participant.Models.domainToLocalStorageView events participations
        }

    let getEventIdByShortname =
        taskResult {
            let! shortnameEncoded = queryParam "shortname"
            let shortname = System.Web.HttpUtility.UrlDecode(shortnameEncoded)
            let! event = Service.getEventByShortname shortname
            return event.Id.Unwrap
        }

    let routes: HttpHandler =
        choose
            [ GET_HEAD
              >=> choose
                      [ route "/events" >=>
                            (checkAsync isAuthenticated
                            >=> handleAsync getEvents
                            |> withTransactionAsync)

                        route "/events/previous" >=>
                            (checkAsync isAuthenticated
                            >=> handleAsync getPastEvents
                            |> withTransaction)

                        routef "/events/%O" (fun eventId -> 
                            checkAsync (eventIsExternalOrUserIsAuthenticated eventId)
                            >=> (handleAsync << getEvent) eventId
                            |> withTransaction)

                        routef "/events/organizer/%s" (fun email -> 
                            checkAsync isAuthenticated
                            >=> (handleAsync << getEventsOrganizedBy) email
                            |> withTransaction)

                        routef "/events-and-participations/%i" (fun id ->
                            checkAsync (isAdminOrAuthenticatedAsUser id)
                            >=> (handleAsync << getEventAndParticipationSummaryForEmployee) id
                            |> withTransaction) 
                        
                        route "/events/id" >=> (handleAsync getEventIdByShortname |> withTransaction)
                      ]
              DELETE
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            checkAsync (userCanEditEvent id)
                            >=> (handleAsync << cancelEvent) id
                            |> withTransaction)
                        routef "/events/%O/delete" (fun id -> 
                            checkAsync (userCanEditEvent id)
                            >=> (handleAsync << deleteEvent) id
                            |> withTransaction)
                        ]
              PUT
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            checkAsync (userCanEditEvent id)
                            >=> (handleAsync << updateEvent) id
                            |> withTransaction) ]
              POST 
              >=> choose 
                    [ route "/events" >=>
                            (checkAsync isAuthenticated
                            >=> handleAsync createEvent 
                            |> withTransaction)] ]
