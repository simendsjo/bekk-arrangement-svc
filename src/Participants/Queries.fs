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

    let deleteParticipant (participant: Participant): AsyncHandler<unit> =
        taskResult {
            let! res =
                delete { table participantsTable
                         where (eq "EventId" participant.EventId.Unwrap + eq "Email" participant.Email.Unwrap) 
                       }
                |> Database.runDeleteQuery 
            return ()
        }

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

    let queryParticipantByKey (eventId: Event.Id, email: EmailAddress): AsyncHandler<Participant> =
        taskResult {
            let! participants =
                select { table participantsTable
                         leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                         where (eq "Participants.EventId" eventId.Unwrap + eq "Participants.Email" email.Unwrap)}
                |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel>

            let participant =
                participants
                |> groupParticipants
                |> Seq.tryHead

            match participant with
            | Some (participant, answers) ->
                return Models.dbToDomain (participant, answers)
            | _ -> 
                return! Error [ UserMessages.participationNotFound (eventId, email.Unwrap) ] |> Task.wrap
        }

    let createParticipant (participant: Participant): AsyncHandler<unit> =
        taskResult {
            let! () =
                queryParticipantByKey (participant.EventId, participant.Email)
                >> Task.map (function
                                | Ok _ -> Error [ UserMessages.participantDuplicate participant.Email.Unwrap ]
                                | Error _ -> Ok ())

            do! insert { table participantsTable
                         value (Models.domainToDb participant)
                       }
                    |> Database.runInsertQuery 
        }

    let queryParticipantionByParticipant (email: EmailAddress): AsyncHandler<Participant seq> =
        taskResult {
            let! participants =
                select { table participantsTable
                         leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                         where (eq "Participants.Email" email.Unwrap)}
                |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel>

            let groupedParticipants =
                participants
                |> groupParticipants
            
            return Seq.map Models.dbToDomain groupedParticipants
        }

    let queryParticipationsByEmployeeId (employeeId: Event.EmployeeId): AsyncHandler<Participant seq> =
        taskResult {
            let! participants =
                select { table participantsTable
                         leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                         where (eq "EmployeeId" employeeId.Unwrap)
                       }
                |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> 

            let groupedParticipants =
                participants
                |> groupParticipants

            return Seq.map Models.dbToDomain groupedParticipants
        }

    let queryParticipantsByEventId (eventId: Event.Id): AsyncHandler<Participant seq> =
        taskResult {
            let! participants =
                select { table participantsTable
                         leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
                         where (eq "Participants.EventId" eventId.Unwrap)
                         orderBy "RegistrationTime" Asc
                       }
                |> Database.runOuterJoinSelectQuery<DbModel, ParticipantAnswerDbModel> 

                // TODO sjekk SQL
            // let sql, values = 
            //     select { table participantsTable
            //              leftJoin answersTable [ "EventId", "Participants.EventId"; "Email", "Participants.Email" ]
            //              where (eq "Participants.EventId" eventId.Unwrap)
            //              orderBy "RegistrationTime" Asc }
            //              |> Deconstructor.select

            //  printfn "%A" sql
            //  printfn "%A" values

            let groupedParticipants =
                participants
                |> groupParticipants

            return Seq.map Models.dbToDomain groupedParticipants
        }
    
    let getNumberOfParticipantsForEvent (eventId: Event.Id): AsyncHandler<int> =
        taskResult {
            let! participants =
                select { table participantsTable
                         count "*" "Value"
                         where (eq "EventId" eventId.Unwrap)
                       }
                |> Database.runSelectQuery<{| Value : int |}>
            
            let count = Seq.tryHead participants
            match count with
            | Some count -> 
                return count.Value
            | None -> 
                return! Error [UserMessages.getParticipantsCountFailed eventId] |> Task.wrap
        }

    let setAnswers (participant: Participant) =
        taskResult {
            if Seq.isEmpty participant.ParticipantAnswers.Unwrap then
                return ()
            else

            let! questions =
                select {
                  table questionsTable
                  where (eq "EventId" participant.EventId.Unwrap)
                  orderBy "Id" Asc
                } |> Database.runSelectQuery<Event.ParticipantQuestionDbModel>

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
                       } |> Database.runInsertQuery
        }