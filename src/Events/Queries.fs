namespace ArrangementService.Event

open System
open ArrangementService.Participant
open Dapper.FSharp
open Microsoft.AspNetCore.Http

open ArrangementService
open ArrangementService.DomainModels
open ArrangementService.UserMessage
open ArrangementService.Event
open ArrangementService.Email
open ArrangementService.ResultComputationExpression

module Queries =
    let eventsTable = "Events"

    let createEvent (event: WriteModel) =
        result {
            let! event = Models.writeToDomain (Guid.NewGuid()) event |> ignoreContext
            let dbModel = Models.domainToDb event
            do! insert { table eventsTable
                         value dbModel
                       }
                |> Database.runInsertQuery
            return Models.dbToDomain dbModel
        }

    let getEvents (ctx: HttpContext): Event seq =
        select { table eventsTable }
        |> Database.runSelectQuery<DbModel> ctx
        |> Seq.map Models.dbToDomain

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

    let queryEventsOrganizedByEmail (organizerEmail: EmailAddress) ctx: Event seq =
        select { table eventsTable
                 where (eq "Email" organizerEmail.Unwrap)
               }
       |> Database.runSelectQuery ctx
       |> Seq.map Models.dbToDomain