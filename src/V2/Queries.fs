module V2.Queries

open System
open ArrangementService
open ArrangementService.Event
open ArrangementService.Participant
open Dapper
open Microsoft.Data.SqlClient

let getEvent (eventId: Guid) (transaction: SqlTransaction) =
    let query =
        "
        SELECT [Id]
              ,[Title]
              ,[Description]
              ,[Location]
              ,[OrganizerEmail]
              ,[StartDate]
              ,[StartTime]
              ,[EndDate]
              ,[EndTime]
              ,[MaxParticipants]
              ,[OrganizerName]
              ,[OpenForRegistrationTime]
              ,[EditToken]
              ,[HasWaitingList]
              ,[IsCancelled]
              ,[IsExternal]
              ,[OrganizerId]
              ,[IsHidden]
              ,[CloseRegistrationTime]
              ,[CustomHexColor]
          FROM [Events]
          WHERE Id = @eventId;
        "
    let parameters = dict [
        "eventId", box eventId
    ]
    
    try
        Ok (transaction.Connection.QuerySingle<Event.DbModel>(query, parameters, transaction))
    with
    | ex -> Error $"Finner ikke arrangement med id: {eventId}. Feil: {ex}"
    
let getNumberOfParticipantsForEvent (eventId: Guid) (transaction: SqlTransaction) =
    let query =
        "
        SELECT COUNT(*) AS NumberOfParticipants
        FROM [Participants]
        WHERE EventId = @eventId;
        "
    let parameters = dict [
        "eventId", box (eventId.ToString())
    ]
    
    transaction.Connection.QuerySingle<int>(query, parameters, transaction)
        
    
let addParticipantToEvent (eventId: Guid) email (userId: int option) name (transaction: SqlTransaction) =
    let query =
        "
        INSERT INTO Participants
        OUTPUT INSERTED.*
        VALUES (@email, @eventId, @currentEpoch, @cancellationToken, @name, @employeeId);
        "
        
    let parameters = dict [
        "eventId", box (eventId.ToString())
        "currentEpoch", box (DateTimeOffset.Now.ToUnixTimeMilliseconds())
        "email", box email
        "cancellationToken", box (Guid.NewGuid().ToString()) 
        "name", box name 
        "employeeId", if userId.IsSome then box userId.Value else box null
    ]
    
    try
        transaction.Connection.QuerySingle<DbModel>(query, parameters, transaction)
        |> Ok
    with
        | ex -> Error $"Kunne ikke legge til deltakeren. Feil {ex}"
    
let getEventQuestions eventId (transaction: SqlTransaction) =
    let query =
        "
        SELECT [Id]
              ,[EventId]
              ,[Question]
        FROM [ParticipantQuestions]
        WHERE EventId = @eventId
        ORDER BY Id ASC;
        "
    let parameters = dict [
        "eventId", box (eventId.ToString())
    ]
    
    transaction.Connection.Query<ParticipantQuestionDbModel>(query, parameters, transaction)
    |> Seq.toList
        
let createParticipantAnswers (participantAnswers: ParticipantAnswerDbModel list) (transaction: SqlTransaction) =
    let answer = List.tryHead participantAnswers
    match answer with
    | None -> Ok []
    | Some answer ->
    let insertQuery =
        "
        INSERT INTO ParticipantAnswers (QuestionId, EventId, Email, Answer)
        VALUES (@QuestionId, @EventId, @Email, @Answer);
        "
    let selectQuery =
        "
        SELECT * FROM ParticipantAnswers WHERE
        QuestionId = @questionId AND EventId = @eventId AND Email = @email;
        "
    let selectParameters = dict [
        "questionId", box answer.QuestionId
        "eventId", box answer.EventId
        "email", box answer.Email
    ]
    
    try
        transaction.Connection.Execute(insertQuery, participantAnswers |> List.toSeq, transaction) |> ignore
        transaction.Connection.Query<ParticipantAnswerDbModel>(selectQuery, selectParameters, transaction)
        |> Seq.toList
        |> Ok
    with
        | ex -> Error $"Kunne ikke legge til svar til deltakeren. Feil {ex}"
    