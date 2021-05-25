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
    let participantsTable = "Participants"

    let createParticipant (participant: Participant) (ctx: HttpContext): Result<Participant, UserMessage list> =
        insert { table participantsTable
                 value (Models.domainToDb participant)
               }
        |> Database.runInsertQuery<DbModel, DbModel> ctx
        |> Seq.tryHead
        |> function
        | Some participant -> Ok <| Models.dbToDomain participant
        // TODO: Add internal server error as an explicit type to UserMessage,
        // potentially with a string explanation - although, this
        // particular case shouldn't happen, so it's hard to explain 🤷‍♂️
        | _ -> Error []

    let deleteParticipant (participant: Participant) (ctx: HttpContext): Result<unit, UserMessage list> =
        delete { table participantsTable
                 where (eq "EventId" participant.EventId.Unwrap + eq "Email" participant.Email.Unwrap) 
               }
        |> Database.runDeleteQuery ctx
        |> ignore
        Ok ()

    let queryParticipantByKey (eventId: Event.Id, email: EmailAddress) ctx: Result<Participant, UserMessage list> =
        select { table participantsTable
                 where (eq "EventId" eventId.Unwrap + eq "Email" email.Unwrap)}
        |> Database.runSelectQuery ctx
        // TODO: Lage en funksjon som gjør dette. Brukes flere plasser, se over.
        |> Seq.tryHead
        |> function
        | Some participant -> Ok <| Models.dbToDomain participant
        | _ -> Error []

    let queryParticipantionByParticipant (email: EmailAddress) ctx: Participant seq =
        select { table participantsTable
                 where (eq "Email" email.Unwrap)
               }
       |> Database.runSelectQuery ctx
       |> Seq.map Models.dbToDomain

    let queryParticipantsByEventId (eventId: Event.Id) ctx: Participant seq =
        select { table participantsTable
                 where (eq "EventId" eventId.Unwrap)
               }
        |> Database.runSelectQuery ctx
        |> Seq.map Models.dbToDomain
