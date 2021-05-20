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

module Queries =

    // TODO: Fix
    // Samme som i Participants
    let createEvent (event: WriteModel) (ctx: HttpContext) =
        let dbConnection = ctx.GetService<IDbConnection>()
        let inserted =
            insert { table "Events"
                     value event
                   }
            |> dbConnection.InsertOutputAsync<WriteModel, {| Id: string |}>
            |> Async.AwaitTask
            |> Async.RunSynchronously
        let id = inserted |> Seq.head |> fun x -> x.Id
        Models.writeToDomain (Guid.Parse id) event 

    let getEvents (ctx: HttpContext): DbModel seq =
        let dbConnection = ctx.GetService<IDbConnection>()
        select { table "Events" }
        |> dbConnection.SelectAsync<DbModel>
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let deleteEvent id (ctx: HttpContext) =
        let dbConnection = ctx.GetService<IDbConnection>()
        delete { table "Events" 
                 where (eq "Id" id)
               }
        |> dbConnection.DeleteAsync
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore
        Ok ()

    let updateEvent id newEvent (ctx: HttpContext) =
        let dbConnection = ctx.GetService<IDbConnection>()
        update { table "Events"
                 set newEvent
                 where (eq "Id" id)
               }
        |> dbConnection.UpdateAsync
        |> Async.AwaitTask
        |> Async.RunSynchronously
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
