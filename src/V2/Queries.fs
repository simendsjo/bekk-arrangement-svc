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
          WHERE Id = @eventId
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
        WHERE EventId = @eventId
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
        VALUES (@email, @eventId, @currentEpoch, @cancellationToken, @name, @employeeId)
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
        ORDER BY Id ASC
        "
    let parameters = dict [
        "eventId", box (eventId.ToString())
    ]
    
    transaction.Connection.Query<ParticipantQuestionDbModel>(query, parameters, transaction)
    |> Seq.toList
        
let createParticipantAnswers (participantAnswers: ParticipantAnswerDbModel list) (transaction: SqlTransaction) =

    // Loop over og lag alle parameter linjene til SQL querien
    let parameters = DynamicParameters()
    let values =
        participantAnswers
        |> List.mapi (fun n answer ->
            // Opprett selve parametrene for Dapper
            parameters.Add($"questionId{n}", answer.QuestionId)
            parameters.Add($"eventId{n}", answer.EventId)
            parameters.Add($"email{n}", answer.Email)
            parameters.Add($"answer{n}", answer.Answer)
            $"(@questionId{n}, @eventId{n}, @email{n}, @answer{n})")
        
    // Slå sammen querien med parameter linjene
    let query =
        $"""
        INSERT INTO ParticipantAnswers (QuestionId, EventId, Email, Answer)
        OUTPUT INSERTED.*
        VALUES {String.concat "," values}
        """
    
    try
        transaction.Connection.Query<ParticipantAnswerDbModel>(query, parameters, transaction)
        |> Seq.toList
        |> Ok
    with
        | ex -> Error $"Kunne ikke legge til svar til deltakeren. Feil {ex}"
    