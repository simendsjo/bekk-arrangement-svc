module V2.Handlers

open System
open System.Web
open ArrangementService.Event
open Giraffe
open Microsoft.Data.SqlClient
open Microsoft.AspNetCore.Http

open ArrangementService
open UserMessage
        
// TODO: EPOST
// TODO: Bli satt på venteliste
// TODO: Fiks kommentarene
// TODO: Error messages
// TODO: Må jeg stenge DB connectionsa?
let registerParticipationHandler3 (eventId: Guid, email): HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let isBekker = context.User.Identity.IsAuthenticated
            let currentEpoch = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            let userId = Auth.getUserIdV2 context
            
            let config = context.GetService<AppConfig>()
            use dbConnection = new SqlConnection(config.databaseConnectionString)
            dbConnection.Open()
            use transaction = dbConnection.BeginTransaction()
            let event = Queries.getEvent eventId transaction
            let numberOfParticipants = Queries.getNumberOfParticipantsForEvent eventId transaction
            
            let! body = context.ReadBodyFromRequestAsync()
            let writeModel = Thoth.Json.Net.Decode.Auto.fromString<Participant.WriteModel> body
            
            let result =
                match event, writeModel with
                | Ok event, Ok writeModel ->
                    // Eventet er ekstern eller det er en bekker som registrer seg
                    if not event.IsExternal && not isBekker then
                       Error "Eventet er ikke eksternt eller du er ikke autentisert"
                    // Eventet er ikke kansellert
                    else if event.IsCancelled then
                       Error "Eventet er kansellert"
                    // Eventet er åpent for registrering
                    else if event.OpenForRegistrationTime >= currentEpoch || (event.CloseRegistrationTime.IsSome && event.CloseRegistrationTime.Value < currentEpoch) then
                        Error "Eventet er ikke åpen for registering"
                    // Eventet har ikke passert
                    else if DateTime.now() > (DateTime.toCustomDateTime event.EndDate event.EndTime) then
                       Error "Eventet tok sted i fortiden"
                    // Eventet har plass
                    else if event.MaxParticipants.IsSome && numberOfParticipants = event.MaxParticipants.Value && not event.HasWaitingList then
                        Error "Eventet har ikke plass"
                    else
                        let insert =    
                            try
                                let participant = Queries.addParticipantToEvent eventId email userId writeModel.Name transaction
                                let answers =
                                    if not (List.isEmpty writeModel.ParticipantAnswers) then
                                        //Queries.createParticipantAnswers eventId email writeModel.ParticipantAnswers transaction
                                        []
                                    else
                                        []
                                Ok (participant, answers)
                            with
                            | ex ->
                                transaction.Rollback()
                                Error $"Kunne ikke legge til denne deltakeren: {ex}"
                                
                        Result.map (fun ((participant: Participant.DbModel), answers) ->
                            transaction.Commit()
                            DomainModels.Participant.CreateFromPrimitives participant.Name participant.Email answers participant.EventId participant.RegistrationTime participant.CancellationToken participant.EmployeeId
                            ) insert
                | Error e, _ -> Error e
                | _, Error e -> Error $"Fikk ikke til å parse request body: {e}"
                
            return!
                match result with
                | Ok result ->
                    let newlyCreatedParticipantViewModel = Participant.Models.domainToViewWithCancelInfo result
                    json newlyCreatedParticipantViewModel next context
                | Error e ->
                    context.SetStatusCode 400
                    json [InternalErrorMessage e] next context
        }