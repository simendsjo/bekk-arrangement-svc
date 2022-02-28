module V2.Queries

open System.Data
open System.Data.SqlClient
open Giraffe

open Microsoft.AspNetCore.Http
open System
open Donald

open ArrangementService
open ArrangementService.DomainModels

let getEvent (eventId: Guid) (context: HttpContext) =
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
          FROM [arrangement-db].[dbo].[Events]
          WHERE Id = @eventId
        "
    let parameters = [
        "eventId", SqlType.Guid eventId
    ]
    let config = context.GetService<AppConfig>()
    // TODO: Hvordan gjør vi dette?
    let dbConnection = new SqlConnection(config.databaseConnectionString)
    
    task {
        let result =
            dbConnection
            |> Db.newCommand query
            |> Db.setParams parameters
            |> Db.querySingle Event.FromReader
            
        dbConnection.Close() 
            
        return
            match result with
            | Ok (Some registration) -> Ok registration
            | Error e -> Error (e.ToString())
            | Ok None -> Error "DB query did not return anything"
    }


// TODO: FLytt denne til et annet sted
type RegistrationResult =
    {
//            StartTime: TimeSpan
        RecordsAffected: int
        OpenForRegistrationTime: int64
        CloseRegistrationTime: int64 option
        MaxParticipants: int option
        IsCancelled: bool
        IsExternal: bool
        NumberOfRegistrations: int option
    }
    static member FromReader (rd: IDataReader) =
        {
            RecordsAffected = rd.RecordsAffected
//                StartTime = "", ""
            OpenForRegistrationTime = rd.ReadInt64 "OpenForRegistrationTime"
            CloseRegistrationTime = rd.ReadInt64Option "CloseRegistrationTime"
            MaxParticipants = rd.ReadInt32Option "MaxParticipants"
            IsCancelled = rd.ReadBoolean "IsCancelled"
            IsExternal = rd.ReadBoolean "IsExternal"
            NumberOfRegistrations = rd.ReadInt32Option "NumberOfRegistrations"
        }

let registerParticipation (eventId: Guid) email (context: HttpContext) =
    let getRegistrationResultQuery = 
        "
        -- Hent ut data for å gi tilbakemelding til brukeren
        --SELECT StartTime, EndDate, OpenForRegistrationTime, CloseRegistrationTime, MaxParticipants, IsCancelled, IsExternal, p.numberOfRegistrations AS NumberOfRegistrations
        SELECT OpenForRegistrationTime, CloseRegistrationTime, MaxParticipants, IsCancelled, IsExternal, p.numberOfRegistrations AS NumberOfRegistrations
        FROM Events as e
        LEFT JOIN (SELECT EventId, Count(*) as numberOfRegistrations from Participants WHERE RegistrationTIme < @currentEpoch GROUP BY EventId) as p on p.EventId = e.Id
        WHERE Id = @eventId
        "
    let query =
        $"
        IF EXISTS(select Id
                FROM Events as e
                        LEFT JOIN (SELECT EventId, count(*) as numParticipants from Participants WHERE EventId = @eventId group by EventId) as part
                                    on part.EventId = e.Id
                WHERE e.Id = @eventId
                -- Er arrangementet ekstern eller er det en Bekker som registerer seg
                AND (e.IsExternal = 1 OR @isBekker = 1)
                -- Eventet er ikke kansellert
                AND (e.IsCancelled = 0)
                -- Eventet er åpent for registrering
                AND (e.OpenForRegistrationTime < (@currentEpoch))
                AND (e.CloseRegistrationTime is null OR e.CloseRegistrationTime > (@currentEpoch))
                -- Eventet har ikke passert
                AND (e.EndDate > SYSUTCDATETIME())
                -- Eventet har plass
                AND (e.MaxParticipants is null OR ((e.HasWaitingList = 1) OR part.numParticipants < e.MaxParticipants)))
            BEGIN
                -- Legg inn deltageren
                INSERT INTO Participants
                VALUES (@email, @eventId, @currentEpoch, @cancellationToken, @name, null)
                
                {getRegistrationResultQuery}
            END
            ELSE
            BEGIN
                {getRegistrationResultQuery}
            END
        "
    let parameters = [
        "eventId", SqlType.String (eventId.ToString())
        "isBekker", SqlType.Boolean context.User.Identity.IsAuthenticated
        "currentEpoch", SqlType.Int64 (DateTimeOffset.Now.ToUnixTimeMilliseconds())
        "email", SqlType.String email
        "cancellationToken", SqlType.String (Guid.NewGuid().ToString())
        "name", SqlType.String "Bjørn-Ivar Strøm"
    ]
    
    let config = context.GetService<AppConfig>()
    // TODO: Hvordan gjør vi dette?
    let dbConnection = new SqlConnection(config.databaseConnectionString)
        
    task {
        let result =
            dbConnection
            |> Db.newCommand query
            |> Db.setParams parameters
            |> Db.querySingle RegistrationResult.FromReader
            
        dbConnection.Close() 
            
        return
            match result with
            | Ok (Some registration) -> Ok registration
            | Error e -> Error (e.ToString())
            | Ok None -> Error "DB query did not return anything"
    }