namespace ArrangementService.Participant

open System
open System.Linq
open Giraffe
open Microsoft.AspNetCore.Http
open System.Data
open System.Collections.Generic
open Dapper.FSharp
open Dapper.FSharp.MSSQL

open ArrangementService
open ArrangementService.Email
open ArrangementService.DomainModels
open ResultComputationExpression
open ArrangementService.UserMessage
open UserMessages

module Queries =

    // TODO: Fix
    // Denne leser ut all dataen
    // og linq metodene under queryer
    // Super slow, skriv om
    let getParticipants (ctx: HttpContext): DbModel seq =
        let foo = ctx.GetService<IDbConnection>()
        select { table "Participants" }
        |> foo.SelectAsync<DbModel>
        |> Async.AwaitTask
        |> Async.RunSynchronously

    // TODO: Fix
    // Denne trenger litt kjærlighet
    // Returnerer nå bare det man får inn
    let createParticipant (participant: WriteModel) (ctx: HttpContext): Result<Participant, UserMessage list> =
        let foo = ctx.GetService<IDbConnection>()
        let inserted =
            insert { table "Participants"
                     value participant
                   }
            |> foo.InsertOutputAsync<WriteModel, {| EventId: string ; Email: string |}>
            |> Async.AwaitTask
            |> Async.RunSynchronously
        let id = inserted |> Seq.head |> fun x -> (Guid.Parse x.EventId, x.Email)
        Models.writeToDomain id participant


    // TODO: Fix
    // skal vi returnere noe? Fire or forget
    let deleteParticipant (participant: DbModel) (ctx: HttpContext): Result<unit, UserMessage list> =
        let foo = ctx.GetService<IDbConnection>()
        delete { table "Participants"
                 where (eq "EventId" participant.EventId + eq "Email" participant.Email) 
               }
        |> foo.DeleteAsync
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore
        Ok ()

    let queryParticipantByKey (id: Event.Id, email: EmailAddress)
        (participants: DbModel seq) =
        query {
            for participant in participants do
                where
                    (participant.Email = email.Unwrap
                     && participant.EventId = id.Unwrap)
                select (Some participant)
                exactlyOneOrDefault
        }
        |> withError [ participationNotFound (id, email) ]

    let queryParticipantionByParticipant (email: EmailAddress)
        (participants: DbModel seq) =
        query {
            for participant in participants do
                where (participant.Email = email.Unwrap)
                select participant
        }

    let queryParticipantsBy (eventId: Event.Id)
        (participants: DbModel seq) =
        query {
            for participant in participants do
                where (participant.EventId = eventId.Unwrap)
                select participant
        }
