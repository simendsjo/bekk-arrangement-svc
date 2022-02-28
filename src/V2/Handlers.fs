module V2.Handlers

open ArrangementService
open ArrangementService.Participant
open ArrangementService.Email
open Giraffe

open Microsoft.AspNetCore.Http
open System.Web
open System
open System.Data
open Donald

open ArrangementService.DomainModels
    
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
        let result = 
            {
    //                StartTime = "", ""
                RecordsAffected = rd.RecordsAffected
                OpenForRegistrationTime = rd.ReadInt64 "OpenForRegistrationTime"
                CloseRegistrationTime = rd.ReadInt64Option "CloseRegistrationTime"
                MaxParticipants = rd.ReadInt32Option "MaxParticipants"
                IsCancelled = rd.ReadBoolean "IsCancelled"
                IsExternal = rd.ReadBoolean "IsExternal"
                NumberOfRegistrations = rd.ReadInt32Option "NumberOfRegistrations"
            }
        rd.NextResult() |> ignore
        result
        
let registerParticipationHandler (eventId: Guid, email): HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let! body = context.ReadBodyFromRequestAsync()
            let writeModel = Thoth.Json.Net.Decode.Auto.fromString<WriteModel> body
            match writeModel with
            | Ok model ->
                let createCancelUrl (participant: Participant) =
                    let redirectUrlTemplate = HttpUtility.UrlDecode model.cancelUrlTemplate
                    redirectUrlTemplate
                        .Replace("{eventId}", participant.EventId.Unwrap.ToString())
                        .Replace("{email}", participant.Email.Unwrap |> Uri.EscapeDataString)
                        .Replace("{cancellationToken}", participant.CancellationToken.ToString())
                let! result = V2.Queries.registerParticipation eventId email context
                let! event = V2.Queries.getEvent eventId context
                let userId = Auth.getUserIdV2 context
                let participant = Models.writeToDomain (eventId, email) model userId
                let config = context.GetService<AppConfig>()
                
                return!
                    match result, event, participant with
                    | Ok registration, Ok event, Ok participant ->
                        if registration.RecordsAffected > 0 then
                            match registration.MaxParticipants, registration.NumberOfRegistrations with
                            | Some maxResult, Some numberOfRegistrations ->
                                if numberOfRegistrations < maxResult then
                                    // Fixme dette kan bli bedre
                                    let email = Service.createNewParticipantMail createCancelUrl event true (EmailAddress config.noReplyEmail) participant
                                    Service.sendMail email context
                                    json "Gikk bra" next context
                                else
                                    let email = Service.createNewParticipantMail createCancelUrl event false (EmailAddress config.noReplyEmail) participant
                                    Service.sendMail email context
                                    json "Gikk bra, Du er på venteliste" next context
                            | _, _ -> 
                                json "Ingen venteliste, alt gikk bra" next context
                        else
                            json "Eventet er fullt" next context
                    | Error e, _, _ -> text $"Feil: {e}" next context
            | Error e -> return! text $"Fikk ikke parset body: {e}" next context
        }
