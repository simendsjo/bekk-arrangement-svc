namespace ArrangementService.Events

open Giraffe

open ArrangementService.Http
open ArrangementService.Operators
open ArrangementService.Repo
open ArrangementService.Events.Models

module Handlers =

    let getEvents =
        result {
            for events in Service.getEvents do
            return Seq.map models.domainToView events 
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            for events in Service.getEventsOrganizedBy organizerEmail do
            return Seq.map models.domainToView events
        }

    let getEvent id =
        result {
            for event in Service.getEvent id do
            return models.domainToView event 
        }

    let deleteEvent id =
        result {
            for result in Service.deleteEvent id do
            yield commitTransaction
            return result 
        }

    let updateEvent id =
        result {
            for writeModel in getBody<WriteModel> do
            let! domainModel = writeToDomain id writeModel

            for updatedEvent in Service.updateEvent id domainModel do
            yield commitTransaction

            return models.domainToView updatedEvent
        }

    let createEvent =
        result {
            for writeModel in getBody<Models.WriteModel> do
            for newEvent in Service.createEvent writeModel do
            return models.domainToView newEvent
        }

    let routes: HttpHandler =
        choose
            [ GET >=> choose
                          [ route "/events" >=> handle getEvents
                            routef "/events/%O" (handle << getEvent)
                            routef "/events/organizer/%s" (handle << getEventsOrganizedBy) ]
              DELETE >=> choose [ routef "/events/%O" (handle << deleteEvent) ]
              PUT >=> choose [ routef "/events/%O" (handle << updateEvent) ]
              POST >=> choose
                           [ route "/events" >=> handle createEvent ] ]
