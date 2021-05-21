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
open ArrangementService.Config

module Queries =

    // TODO: Fix
    // Denne leser ut all dataen
    // og linq metodene under queryer
    // Super slow, skriv om
    let getParticipants (ctx: HttpContext): DbModel seq =
        select { table "Participants" }
        |> Database.runSelectQuery<DbModel> ctx

    // TODO: Fix
    // Denne trenger litt kjærlighet
    // Returnerer nå bare det man får inn
    let createParticipant (participant: WriteModel) (ctx: HttpContext): Result<Participant, UserMessage list> =
        let inserted =
            insert { table "Participants"
                     value participant
                   }
            |> Database.runInsertQuery<WriteModel, {| EventId: string ; Email: string |}> ctx
        let id = inserted |> Seq.head |> fun x -> (Guid.Parse x.EventId, x.Email)
        Models.writeToDomain id participant


    // TODO: Fix
    // skal vi returnere noe? Fire or forget
    let deleteParticipant (participant: DbModel) (ctx: HttpContext): Result<unit, UserMessage list> =
        delete { table "Participants"
                 where (eq "EventId" participant.EventId + eq "Email" participant.Email) 
               }
        |> Database.runDeleteQuery ctx
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
