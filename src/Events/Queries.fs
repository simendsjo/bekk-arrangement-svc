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

    let runSelectQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.SelectAsync<DbModel>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runUpdateQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.UpdateAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runDeleteQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.DeleteAsync(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let runInsertQuery (ctx: HttpContext) query =
        let config = getConfig ctx
        config.currentConnection.InsertOutputAsync<WriteModel, {| Id: string |}>(query, config.currentTransaction)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        

    // TODO: Fix
    // Samme som i Participants
    let createEvent (event: WriteModel) (ctx: HttpContext) =
        let inserted =
            insert { table "Events"
                     value event
                   }
            |> runInsertQuery ctx
        let id = inserted |> Seq.head |> fun x -> x.Id
        Models.writeToDomain (Guid.Parse id) event 

    let getEvents (ctx: HttpContext): DbModel seq =
        select { table "Events "}
        |> runSelectQuery ctx

    let deleteEvent id (ctx: HttpContext) =
        delete { table "Events" 
                 where (eq "Id" id)
               }
        |> runDeleteQuery ctx
        |> ignore
        Ok ()

    let updateEvent id newEvent (ctx: HttpContext) =
        update { table "Events"
                 set newEvent
                 where (eq "Id" id)
               }
        |> runUpdateQuery ctx
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
