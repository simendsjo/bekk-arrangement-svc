namespace kaSkjerSvc.Services

open kaSkjerSvc.Models
open kaSkjerSvc.Repositories

module EventService =
    let getEvents () = EventRepository.getEvents ()
                       |> Seq.map EventModels.mapDomainEventToView

    let getEventsForEmployee employeeId =
      EventRepository.getEventsForEmployee employeeId
      |> Seq.map EventModels.mapDomainEventToView
                                          
    let getEvent id = EventRepository.getEvent id
                      |> Option.map EventModels.mapDomainEventToView
                      
    let deleteEvent id = EventRepository.deleteEvent id
    
    let updateEvent event = EventRepository.updateEvent event
                            |> Option.map EventModels.mapDomainEventToView
    
    let createEvent event = EventRepository.createEvent event
                           |> EventModels.mapDomainEventToView