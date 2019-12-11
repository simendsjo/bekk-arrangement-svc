namespace ArrangementService.Events

open Giraffe

open ArrangementService.Http
open ArrangementService.Operators
open ArrangementService.Repo
open ArrangementService

module Handlers =

    let eventModels = Events.Models.models
    let participantModels = Participants.Models.models

    let getEvents =
        Service.getEvents
        >> Seq.map eventModels.domainToView
        >> Ok

    //    let getEventsForEmployee employeeId =
    //        Service.getEventsForEmployee employeeId
    //        >> Seq.map models.domainToView
    //        >> Ok

    let getEvent = Service.getEvent

    let deleteEvent id = Service.deleteEvent id >>= sideEffect commitTransaction

    let updateEvent id =
        getBody<Events.Models.WriteModel>
        >> Result.map (eventModels.writeToDomain id)
        >>= Service.updateEvent id
        >>= sideEffect commitTransaction
        >> Result.map eventModels.domainToView

    let createEvent =
        getBody<Events.Models.WriteModel>
        >>= Service.createEvent
        >> Result.map eventModels.domainToView

    let registerForEvent id = 
        getBody<Participants.Models.WriteModel> 
        >> Result.map (participantModels.writeToDomain (id, ""))
        >>= Service.registerParticipant
        >> Result.map participantModels.domainToView

    let routes: HttpHandler =
        choose
            [ GET >=> choose
                          [ route "/events" >=> handle getEvents
                            routef "/events/%O" (handle << getEvent) ]
              //                            routef "/events/employee/%i" (handle << getEventsForEmployee) ]
              DELETE >=> choose [ routef "/events/%O" (handle << deleteEvent) ]
              PUT >=> choose [ routef "/events/%O" (handle << updateEvent) ]
              POST >=> choose
                           [ route "/events" >=> handle createEvent 
                             routef "/events/register/%O" (handle << registerForEvent) ] ]
