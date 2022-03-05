module V2.Queries

open System
open ArrangementService
open ArrangementService
open Dapper
open Microsoft.Data.SqlClient

let getEvent (eventId: Guid) (transaction: SqlTransaction) =
    let query =
        "
        SELECT [Id]
              ,[Title]
              ,[Description]
              ,[Location]
              ,[StartDate]
              ,[StartTime]
              ,[EndDate]
              ,[EndTime]
              ,[OrganizerName]
              ,[OrganizerEmail]
              ,[OpenForRegistrationTime]
              ,[CloseRegistrationTime]
              ,[MaxParticipants]
              ,[EditToken]
              ,[HasWaitingList]
              ,[IsCancelled]
              ,[IsExternal]
              ,[IsHidden]
              ,[OrganizerId]
              ,[CustomHexColor]
          FROM Events
          WHERE Id = @eventId
        "
    let parameters = dict [
        "eventId", box (eventId.ToString())
    ]
    
    try
        Ok (transaction.Connection.QuerySingle<Event.DbModel>(query, parameters, transaction))
    with
    | _ -> Error $"Finner ikke arrangement med id: {eventId}"
    
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
    
    transaction.Connection.QuerySingle<Participant.DbModel>(query, parameters, transaction)
    
//let createParticipantAnswers (eventId: Guid) email participantAnswers (transaction: SqlTransaction) =
//
//    let dbModels: Participant.DomainTypes.ParticipantAnswerDbModel list =
//        participantAnswers
//        |> List.map (fun (answer: Participant.DomainTypes.Answer) ->
//            { QuestionId = answer.Id
//              EventId = eventId
//              Email = email
//              Answer = answer.Answer
//            })
//    // Loop over og lag alle parameter linjene til SQL querien
//    let parameters = DynamicParameters()
//    let values =
//        dbModels
//        |> List.mapi (fun n answer ->
//            // Opprett selve parametrene for Dapper
//            parameters.Add($"questionId{n}", answer.QuestionId)
//            parameters.Add($"eventId{n}", eventId)
//            parameters.Add($"email{n}", email)
//            parameters.Add($"answer{n}", answer.Answer)
//            $"(@questionId{n}, @eventId{n}, @email{n}, @answer{n})")
//        
//    // Slå sammen querien med parameter linjene
//    let query =
//        $"""
//        INSERT INTO ParticipantAnswers (QuestionId, EventId, Email, Answer)
//        OUTPUT INSERTED.*
//        VALUES {String.concat "," values}
//        """
//    
//    transaction.Connection.Query<Participant.DomainTypes.ParticipantAnswerDbModel>(query, parameters, transaction)
//    |> Seq.toList