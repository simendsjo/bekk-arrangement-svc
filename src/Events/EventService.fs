namespace arrangementSvc.Services

open arrangementSvc.Models
open arrangementSvc.Repositories
open arrangementSvc.Database

module EventService =
    let getEvents (dbContext : ArrangementDbContext) =
        EventRepository.getEvents dbContext |> Seq.map EventModels.mapDomainEventToView

    let getEventsForEmployee employeeId (dbContext : ArrangementDbContext) =
      EventRepository.getEventsForEmployee employeeId dbContext
      |> Seq.map EventModels.mapDomainEventToView
                                          
    let getEvent id (dbContext : ArrangementDbContext) =
        EventRepository.getEvent id dbContext |> Option.map EventModels.mapDomainEventToView
                      
    let deleteEvent = EventRepository.deleteEvent
    
    let updateEvent event (dbContext : ArrangementDbContext) =
        EventRepository.updateEvent event dbContext |> Option.map EventModels.mapDomainEventToView
    
    let createEvent event (dbContext : ArrangementDbContext) =
        EventRepository.createEvent event dbContext |> EventModels.mapDomainEventToView