namespace ArrangementService.Participant

open System
open Microsoft.AspNetCore.Http
open Dapper.FSharp

open ArrangementService
open ArrangementService.Email
open ArrangementService.DomainModels
open ResultComputationExpression
open ArrangementService.UserMessage
open System.Data.SqlClient
open ArrangementService.Event.Queries
open ArrangementService.Tools

module Queries =
    let participantsTable = "Participants"
    let answersTable = "ParticipantAnswers"

    let handleAggregateToSqlException (exn:AggregateException) userMessage = 
          let innerException = exn.InnerException
          match innerException with 
            | :? SqlException as sqlEx ->
                match sqlEx.Number with
                  | 2601 | 2627 ->          // handle constraint error
                      Error [userMessage]
                  | _ ->                    // don't handle any other cases, Deadlock will still be raised so it can be catched by withRetry
                      raise sqlEx
            | ex -> raise ex

    let createParticipant (participant: Participant) (ctx:HttpContext):Result<unit, UserMessage list> =
      try
        insert { table participantsTable
                 value (Models.domainToDb participant)
                }
                |> Database.runInsertQuery ctx
      with  
      | :? AggregateException as ex  -> 
        handleAggregateToSqlException ex (UserMessages.participantDuplicate participant.Email.Unwrap)
      | ex -> reraise()

    let deleteParticipant (participant: Participant) (ctx: HttpContext): Result<unit, UserMessage list> =
        delete { table participantsTable
                 where (eq "EventId" participant.EventId.Unwrap + eq "Email" participant.Email.Unwrap) 
               }
        |> Database.runDeleteQuery ctx
        |> ignore
        Ok ()

    let private groupParticipants ls =
        ls
        |> Seq.groupBy (fun (participant: DbModel, _) -> (participant.EventId, participant.Email))
        |> Seq.map (fun (_, listOfParticipants) -> 
            let (participant, _) = listOfParticipants |> Seq.head 
            let sortedAnswersForParticipant =
                listOfParticipants 
                |> Seq.collect (fun (_, answer) -> match answer with | Some a -> [ a ] | None -> []) 
                |> Seq.sortBy (fun a -> a.QuestionId) 
                |> Seq.map (fun a -> a.Answer) 
                |> List.ofSeq

            (participant , sortedAnswersForParticipant)
        )

    let queryParticipantByKey (eventId: Event.Id, email: EmailAddress) ctx: Result<Participant, UserMessage list> =
        select { table participantsTable
                 leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                 where (eq "Participants.EventId" eventId.Unwrap + eq "Participants.Email" email.Unwrap)}
        |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> ctx
        |> groupParticipants
        |> Seq.tryHead
        |> function
        | Some (participant, answers) -> Ok <| Models.dbToDomain (participant, answers)
        | _ -> Error []

    let queryParticipantionByParticipant (email: EmailAddress) ctx: Participant seq =
        select { table participantsTable
                 leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                 where (eq "Participants.Email" email.Unwrap)}
        |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> ctx
        |> groupParticipants
        |> Seq.map Models.dbToDomain

    let queryParticipationsByEmployeeId (employeeId: Event.EmployeeId) ctx: Participant seq =
        select { table participantsTable
                 leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                 where (eq "EmployeeId" employeeId.Unwrap)
               }
        |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> ctx
        |> groupParticipants
        |> Seq.map Models.dbToDomain

    let queryParticipantsByEventId (eventId: Event.Id) ctx: Participant seq =
        select { table participantsTable
                 leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                 where (eq "Participants.EventId" eventId.Unwrap)
                 orderBy "RegistrationTime" Asc
               }
        |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> ctx
        |> groupParticipants
        |> Seq.map Models.dbToDomain
    
    let getNumberOfParticipantsForEvent (eventId: Event.Id) (ctx: HttpContext) =
        select { table participantsTable
                 count "*" "Value"
                 where (eq "EventId" eventId.Unwrap)
               }
        |> Database.runSelectQuery<{| Value : int |}> ctx
        |> Seq.tryHead
        |> function
        | Some count -> Ok count.Value
        | None -> Error [UserMessages.getParticipantsCountFailed eventId]

    let setAnswers (participant: Participant) =
        result {
            if Seq.isEmpty participant.ParticipantAnswers.Unwrap then
                return ()
            else

            let! questions =
                select {
                  table questionsTable
                  where (eq "EventId" participant.EventId.Unwrap)
                  orderBy "Id" Asc
                } |> flip Database.runSelectQuery<Event.ParticipantQuestionDbModel>
                >> Ok

            do! insert { table answersTable
                         values (participant.ParticipantAnswers.Unwrap 
                                |> Seq.zip questions
                                |> Seq.map (fun (question, answer) -> 
                                    {| QuestionId = question.Id
                                       EventId = participant.EventId.Unwrap
                                       Email = participant.Email.Unwrap
                                       Answer = answer
                                    |})
                                |> List.ofSeq)
                       } |> flip Database.runInsertQuery
        }