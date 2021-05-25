namespace ArrangementService.Event

open UserMessages
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

module Queries =
    let eventsTable = "Events"

    // TODO: Fix
    // Samme som i Participants
    let createEvent (event: WriteModel) (ctx: HttpContext) =
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

    let deleteEvent id (ctx: HttpContext) =
        delete { table eventsTable
                 where (eq "Id" id)
               }
        |> Database.runDeleteQuery ctx
        |> ignore
        Ok ()

    let updateEvent id newEvent (ctx: HttpContext) =
        update { table eventsTable
                 set newEvent
                 where (eq "Id" id)
               }
        |> Database.runUpdateQuery ctx
        |> ignore
        Ok ()
             
    let queryEventBy (id: Id) (events: DbModel seq) =
        query {
            for event in events do
                where (event.Id = id.Unwrap)
                select (Some event)
                exactlyOneOrDefault
        }
        |> withError [ eventNotFound id ]

    let queryEventsOrganizedBy (organizerEmail: string)
        (events: DbModel seq) =
        query {
            for event in events do
                where (event.OrganizerEmail = organizerEmail)
                select event
        }
