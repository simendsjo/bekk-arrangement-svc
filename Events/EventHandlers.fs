namespace arrangementSvc.Handlers

open Giraffe
open Microsoft.AspNetCore.Http

open arrangementSvc.Models.EventModels
open arrangementSvc.Services
open arrangementSvc.Database

module EventHandlers =    
    let getEvents (next : HttpFunc) (ctx : HttpContext) =
        let dbContext = ctx.GetService<ArrangementDbContext>()
        json (EventService.getEvents dbContext) next ctx
        
    let getEventsForEmployee employeeId : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let dbContext = ctx.GetService<ArrangementDbContext>()
            json (EventService.getEventsForEmployee employeeId dbContext) next ctx

    let getEvent id : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let dbContext = ctx.GetService<ArrangementDbContext>()
            EventService.getEvent id dbContext
            |> function
                | Some event -> json event next ctx
                | None -> RequestErrors.NOT_FOUND "Event not found" next ctx
    
    let deleteEvent id : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let dbContext = ctx.GetService<ArrangementDbContext>()
            EventService.deleteEvent id dbContext
            |> function
                | Some _ -> Successful.OK "Event deleted ok" next ctx
                | None -> RequestErrors.NOT_FOUND "Event not found" next ctx
                
    let updateEvent id : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                let dbContext = ctx.GetService<ArrangementDbContext>()
                let event = ctx.BindJsonAsync<EventWriteModel>().Result
                EventService.updateEvent (event |> mapWriteEventToDomain id) dbContext
                |> function
                    | Some event -> json event next ctx
                    | None -> RequestErrors.NOT_FOUND "Event not found" next ctx
                
    let createEvent (next : HttpFunc) (ctx : HttpContext) =
        let dbContext = ctx.GetService<ArrangementDbContext>()
        let event = ctx.BindJsonAsync<EventWriteModel>().Result
        let newEvent = EventService.createEvent event dbContext
        json newEvent next ctx    
    
    let EventRoutes : HttpHandler =
        choose [
            GET >=> choose[
                route "/events" >=> getEvents
                routef "/events/id=%i" getEvent
                routef "/events/employeeId=%i" getEventsForEmployee
            ]
            DELETE >=> choose[
                routef "/events/id=%i" deleteEvent
            ]
            PUT >=> choose [
                routef "/events/id=%i" updateEvent
            ]
            POST >=> choose [
                route "/events" >=> createEvent
            ]
        ]
