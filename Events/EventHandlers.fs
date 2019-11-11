namespace kaSkjerSvc.Handlers

open Giraffe
open Microsoft.AspNetCore.Http

open kaSkjerSvc.Models.EventModels
open kaSkjerSvc.Services

module EventHandlers =    
    let getEvents (next : HttpFunc) (ctx : HttpContext) =
        json (EventService.getEvents ()) next ctx
        
    let getEventsForEmployee employeeId : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            json (EventService.getEventsForEmployee employeeId) next ctx

    let getEvent id : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            EventService.getEvent id
            |> function
                | Some event -> json event next ctx
                | None -> RequestErrors.NOT_FOUND "Event not found" next ctx
    
    let deleteEvent id : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            EventService.deleteEvent id
            |> function
                | Some _ -> Successful.OK "Event deleted ok" next ctx
                | None -> RequestErrors.NOT_FOUND "Event not found" next ctx
                
                
    let updateEvent id : HttpHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                let event = ctx.BindJsonAsync<EventWriteModel>().Result
                EventService.updateEvent (event |> mapWriteEventToDomain id)
                |> function
                    | Some event -> json event next ctx
                    | None -> RequestErrors.NOT_FOUND "Event not found" next ctx
                
    let createEvent (next : HttpFunc) (ctx : HttpContext) =
        let event = ctx.BindJsonAsync<EventWriteModel>().Result
        let newEvent = EventService.createEvent event
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
