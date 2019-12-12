namespace ArrangementService.Events

open Giraffe

open ArrangementService.Http
open ArrangementService.Operators
open ArrangementService.Repo

open Models

module Handlers =

    let getEvents =
        result {
            let! events = Service.getEvents 
            return Seq.map models.domainToView events |> ignoreContext
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! events = Service.getEventsOrganizedBy organizerEmail
            return Seq.map models.domainToView events |> ignoreContext
        }

    let getEvent id =
        result {
            let! event = Service.getEvent id
            return models.domainToView event |> ignoreContext
        }

    let deleteEvent id =
        result {
            let! result = Service.deleteEvent id
            do! commitTransaction >> Ok
            return result |> ignoreContext
        }

    let updateEvent id =
        result {
            let! writeModel = getBody<WriteModel>
            let! domainModel = writeToDomain (Id id) writeModel |> ignoreContext
            let! updatedEvent = Service.updateEvent id domainModel
            do! commitTransaction >> Ok
            return models.domainToView updatedEvent |> ignoreContext
        }

    let createEvent =
        result {
            let! writeModel = getBody<Models.WriteModel>
            let! newEvent = Service.createEvent writeModel
            return models.domainToView newEvent |> ignoreContext
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
