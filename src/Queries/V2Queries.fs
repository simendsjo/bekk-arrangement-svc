module V2.Queries

open System
open Dapper
open Microsoft.Data.SqlClient

[<CLIMutable>]
type private ForsideEvent = {
    Id: Guid
    Title: string
    Location: string
    StartDate: DateTime
    StartTime: TimeSpan
    MaxParticipants: int option
    CustomHexColor: string option
    NumberOfParticipants: int
    IsParticipating: bool
}

let getEventsForForside (email: string) (transaction: SqlTransaction) =
    task {
        let query =
            "
            SELECT E.Id, E.Title, E.Location, E.StartDate, E.StartTime, E.MaxParticipants, E.CustomHexColor, COUNT(P.EmployeeId) as NumberOfParticipants, (SELECT COUNT(*) FROM Participants p0 WHERE p0.Email = @email AND p0.EventId = E.Id) as IsParticipating
            FROM Events E
            LEFT JOIN Participants P on E.Id = P.EventId
            WHERE EndDate > GETDATE() AND IsCancelled = 0
            GROUP BY E.Id, E.Title, E.Location, E.StartDate, E.StartTime, E.MaxParticipants, E.CustomHexColor
            "
            
        let parameters = dict [
            "email", box email
        ]
        
        try
            let! result = transaction.Connection.QueryAsync<FOO>(query, parameters, transaction)
            return Ok (Seq.toList result)
        with
        | ex ->
            return Error $"Klarer ikke hente arrangementer. Feil: {ex}"
    }

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
        Ok (transaction.Connection.QuerySingle<Event.Models.DbModel>(query, parameters, transaction))
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
        transaction.Connection.QuerySingle<Participant.Models.DbModel>(query, parameters, transaction)
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
    
    transaction.Connection.Query<Event.Models.ParticipantQuestionDbModel>(query, parameters, transaction)
    |> Seq.toList
        
let createParticipantAnswers (participantAnswers: Participant.Models.ParticipantAnswerDbModel list) (transaction: SqlTransaction) =
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
        transaction.Connection.Query<Participant.Models.ParticipantAnswerDbModel>(selectQuery, selectParameters, transaction)
        |> Seq.toList
        |> Ok
    with
        | ex -> Error $"Kunne ikke legge til svar til deltakeren. Feil {ex}"
    