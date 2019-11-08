namespace kaSkjerSvc.Handlers

open Giraffe
open Microsoft.AspNetCore.Http

open Giraffe.HttpStatusCodeHandlers
open kaSkjerSvc.Models
open kaSkjerSvc.Services

module EventHandlers =
    let getEvents (next : HttpFunc) (ctx : HttpContext) =
        json (EventService.getEvents ()) next ctx
        
    let getEventsForEmployee employeeId : HttpHandler =
        fun (next: HttpFunc) (ctx : HttpContext) ->
            json (EventService.getEventsForEmployee employeeId) next ctx

    let getEvent id : HttpHandler =
        fun (next: HttpFunc) (ctx : HttpContext) ->
            EventService.getEvent id
            |> function
                | Some event -> json event next ctx
                | None -> RequestErrors.NOT_FOUND "Event not found" next ctx
    
    let deleteEvent id : HttpHandler =
        fun (next: HttpFunc) (ctx : HttpContext) ->
            EventService.deleteEvent id
            |> function
                | Some _ -> Successful.OK "Event deleted ok" next ctx
                | None -> RequestErrors.NOT_FOUND "Event not found" next ctx