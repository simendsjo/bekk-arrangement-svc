namespace ArrangementService.Event

open ArrangementService

open Http
open ResultComputationExpression
open Repo
open Models
open ArrangementService.DomainModels
open Authorization

open Giraffe
open System.Web

module Handlers =

    let getEvents =
        result {
            for events in Service.getEvents do
                return Seq.map domainToView events |> Seq.toList
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            for events in Service.getEventsOrganizedBy organizerEmail do
                return Seq.map domainToView events |> Seq.toList
        }

    let getEvent id =
        result {
            for event in Service.getEvent (Id id) do
                return domainToView event
        }

    let deleteEvent id =
        result {
            for result in Service.deleteEvent (Id id) do
                yield commitTransaction
                return result
        }

    let updateEvent id =
        result {
            for writeModel in getBody<WriteModel> do
                let! domainModel = writeToDomain id writeModel

                for updatedEvent in Service.updateEvent (Id id) domainModel do
                    yield commitTransaction
                    return domainToView updatedEvent
        }

    let createEvent =
        result {
            for writeModel in getBody<WriteModel> do

                let redirectUrlTemplate =
                    HttpUtility.UrlDecode writeModel.editUrlTemplate

                let createEditUrl (event: Event) =
                    redirectUrlTemplate.Replace("{eventId}",
                                                event.Id.Unwrap.ToString())
                                       .Replace("{editToken}",
                                                event.EditToken.ToString())

                for newEvent in Service.createEvent createEditUrl
                                    (fun id -> writeToDomain id writeModel) do

                    return domainToViewWithEditInfo newEvent
        }

    let routes: HttpHandler =
        choose
            [ GET
              >=> choose
                      [ route "/events" >=> handle getEvents
                        routef "/events/%O" (handle << getEvent)
                        routef "/events/organizer/%s"
                            (handle << getEventsOrganizedBy) ]
              DELETE
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << deleteEvent) id) ]
              PUT
              >=> choose
                      [ routef "/events/%O" (fun id ->
                            check (userCanEditEvent id)
                            >=> (handle << updateEvent) id) ]
              POST >=> choose [ route "/events" >=> handle createEvent ] ]
