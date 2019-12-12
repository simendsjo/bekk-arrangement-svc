namespace ArrangementService.Events

open Giraffe

open ArrangementService.Http
open ArrangementService.Operators
open ArrangementService.Repo

open Models

module Handlers =

    let getEvents ctx =
        result {
            let! events = Service.getEvents ctx
            return Seq.map models.domainToView events
        }

    let getEventsOrganizedBy organizerEmail ctx =
        result {
            let! events = Service.getEventsOrganizedBy organizerEmail ctx
            return Seq.map models.domainToView events
        }

    let getEvent ctx =
        result {
            return! Service.getEvent ctx
        }

    let deleteEvent id ctx =
        result {
            let! result = Service.deleteEvent id ctx
            commitTransaction ctx
            return result
        }

    let updateEvent id ctx =
        result {
            let! writeModel = getBody<WriteModel> ctx
            let! domainModel = writeToDomain (Id id) writeModel
            let! updatedEvent = Service.updateEvent id domainModel ctx
            commitTransaction ctx
            return models.domainToView updatedEvent
        }

    let createEvent ctx =
        result {
            let! writeModel = getBody<Models.WriteModel> ctx
            let! newEvent = Service.createEvent writeModel ctx
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
              POST >=> choose [ route "/events" >=> handle createEvent ] ]
