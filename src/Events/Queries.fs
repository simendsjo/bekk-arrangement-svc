namespace ArrangementService.Event

open System.Linq
open System
open System.Data
open Dapper.FSharp
open Dapper.FSharp.MSSQL
open Microsoft.AspNetCore.Http
open Giraffe

open ArrangementService
open ResultComputationExpression
open ArrangementService.Config
open ArrangementService.DomainModels
open ArrangementService.UserMessage
open ArrangementService.Event

module Queries =
    let eventsTable = "Events"

    // TODO: Fix
    // Samme som i Participants
    let createEvent (event: WriteModel) (ctx: HttpContext): Result<Event, UserMessage list> =
        let inserted =
            insert { table eventsTable
                     value event
                   }
            |> Database.runInsertQuery<WriteModel, {| Id: string |}> ctx
        let id = inserted |> Seq.head |> fun x -> x.Id
        Models.writeToDomain (Guid.Parse id) event 

    let getEvents (ctx: HttpContext): DbModel seq =
        select { table eventsTable }
        |> Database.runSelectQuery<DbModel> ctx

    let deleteEvent (id: Event.Id) (ctx: HttpContext): Result<Unit, UserMessage list> =
        delete { table eventsTable
                 where (eq "Id" id.Unwrap)
               }
        |> Database.runDeleteQuery ctx
        |> ignore
        Ok ()

    let updateEvent (id: Event.Id) (newEvent: Event) (ctx: HttpContext): Result<Unit, UserMessage list> =
        update { table eventsTable
                 set newEvent
                 where (eq "Id" id.Unwrap)
               }
        |> Database.runUpdateQuery ctx
        |> ignore
        Ok ()

    let queryEventByEventId (eventId: Event.Id) ctx: Result<Event, UserMessage list> =
        select { table eventsTable 
                 where (eq "Id" eventId.Unwrap)
               }
       |> Database.runSelectQuery ctx
       |> Seq.tryHead
       |> function
       | Some event -> Ok <| Models.dbToDomain event
       | None -> Error [ UserMessages.eventNotFound eventId ]
             
    let queryEventsOrganizedBy (organizerEmail: string)
        (events: DbModel seq) =
        query {
            for event in events do
                where (event.OrganizerEmail = organizerEmail)
                select event
        }
