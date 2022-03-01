module V2.Handlers

open ArrangementService
open ArrangementService.Event
open ArrangementService.Participant
open ArrangementService.Email
open Giraffe

open Microsoft.AspNetCore.Http
open System.Web
open System
open System.Data
open Donald

open ArrangementService.DomainModels
open Microsoft.Data.SqlClient
    
//type RegistrationResult =
//    {
////            StartTime: TimeSpan
//        RecordsAffected: int
//        OpenForRegistrationTime: int64
//        CloseRegistrationTime: int64 option
//        MaxParticipants: int option
//        IsCancelled: bool
//        IsExternal: bool
//        NumberOfRegistrations: int option
//    }
//    static member FromReader (rd: IDataReader) =
//        let result = 
//            {
//    //                StartTime = "", ""
//                RecordsAffected = rd.RecordsAffected
//                OpenForRegistrationTime = rd.ReadInt64 "OpenForRegistrationTime"
//                CloseRegistrationTime = rd.ReadInt64Option "CloseRegistrationTime"
//                MaxParticipants = rd.ReadInt32Option "MaxParticipants"
//                IsCancelled = rd.ReadBoolean "IsCancelled"
//                IsExternal = rd.ReadBoolean "IsExternal"
//                NumberOfRegistrations = rd.ReadInt32Option "NumberOfRegistrations"
//            }
//        rd.NextResult() |> ignore
//        result
        
let registerParticipationHandler3 (eventId: Guid, email): HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let config = context.GetService<AppConfig>()
            let dbConnection = new SqlConnection(config.databaseConnectionString)
            let event = V2.Queries.getEvent eventId dbConnection
            let numberOfParticipants = V2.Queries.getNumberOfParticipantsForEvent eventId dbConnection
            let isBekker = context.User.Identity.IsAuthenticated
            let currentEpoch = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            
            let result =
                match event, numberOfParticipants with
                | Ok event, Ok numberOfParticipants ->
                    // Eventet er ekstern eller det er en bekker som registrer seg
                    if not event.IsExternal && not isBekker then
                       Error "Eventet er ikke eksternt eller du er ikke autentisert"
                    // Eventet er ikke kansellert
                    else if event.IsCancelled then
                       Error "Eventet er kansellert"
                    // Eventet er åpent for registrering
                    else if event.OpenForRegistrationTime.Unwrap >= currentEpoch || (event.CloseRegistrationTime.Unwrap.IsSome && event.CloseRegistrationTime.Unwrap.Value < currentEpoch) then
                        Error "Eventet er ikke åpen for registering"
                    // Eventet har ikke passert
                    else if DateTime.now() < event.EndDate then
                       Error "Eventet tok sted i fortiden"
                    // Eventet har plass
                    else if event.MaxParticipants.Unwrap.IsSome && numberOfParticipants > event.MaxParticipants.Unwrap.Value || event.HasWaitingList then
                    // TODO: Legg inn deltageren
                        Error "Eventet har ikke plass"
                    else
                        use transaction = dbConnection.TryBeginTransaction()
                        V2.Queries.addParticipantToEvent eventId email dbConnection transaction
                        transaction.TryCommit()
                        V2.Queries.readParticipantFromEvent eventId email dbConnection
                        
                | Error e, _ -> Error e
                
            dbConnection.Close()
                
            return!
                match result with
                | Ok good ->
                    json good next context
                | Error e ->
                    context.SetStatusCode 400
                    text e next context
        }
    
        
//let registerParticipationHandler (eventId: Guid, email): HttpHandler =
//    fun (next: HttpFunc) (context: HttpContext) ->
//        task {
//            let! body = context.ReadBodyFromRequestAsync()
//            let writeModel = Thoth.Json.Net.Decode.Auto.fromString<WriteModel> body
//            match writeModel with
//            | Ok model ->
//                let createCancelUrl (participant: Participant) =
//                    let redirectUrlTemplate = HttpUtility.UrlDecode model.cancelUrlTemplate
//                    redirectUrlTemplate
//                        .Replace("{eventId}", participant.EventId.Unwrap.ToString())
//                        .Replace("{email}", participant.Email.Unwrap |> Uri.EscapeDataString)
//                        .Replace("{cancellationToken}", participant.CancellationToken.ToString())
//                let! result = V2.Queries.registerParticipation eventId email context
//                let! event = V2.Queries.getEvent eventId context
//                let userId = Auth.getUserIdV2 context
//                let participant = Models.writeToDomain (eventId, email) model userId
//                let config = context.GetService<AppConfig>()
//                
//                return!
//                    match result, event, participant with
//                    | Ok registration, Ok event, Ok participant ->
//                        if registration.RecordsAffected > 0 then
//                            match registration.MaxParticipants, registration.NumberOfRegistrations with
//                            | Some maxResult, Some numberOfRegistrations ->
//                                if numberOfRegistrations < maxResult then
//                                    let email = Service.createNewParticipantMail createCancelUrl event true (EmailAddress config.noReplyEmail) participant
//                                    Service.sendMail email context
//                                    json "Gikk bra" next context
//                                else
//                                    let email = Service.createNewParticipantMail createCancelUrl event false (EmailAddress config.noReplyEmail) participant
//                                    Service.sendMail email context
//                                    json "Gikk bra, Du er på venteliste" next context
//                            | _, _ -> 
//                                json "Ingen venteliste, alt gikk bra" next context
//                        else
//                            json "Eventet er fullt" next context
//                    | Error e, _, _ -> text $"Feil: {e}" next context
//            | Error e -> return! text $"Fikk ikke parset body: {e}" next context
//        }
