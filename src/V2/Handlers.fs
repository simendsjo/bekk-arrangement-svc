module V2.Handlers

open System
open ArrangementService.Event
open ArrangementService.Participant
open Giraffe
open Microsoft.Data.SqlClient
open Microsoft.AspNetCore.Http

open ArrangementService
open Thoth.Json.Net
open UserMessage
        
// TODO: EPOST
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
            let writeModel = Decode.Auto.fromString<WriteModel> (body, caseStrategy = CamelCase)
            
            let result =
                match event, writeModel with
                | Ok event, Ok writeModel ->
                    // Nedenfor er en rekke tester som skal feile og gi en feilmelding dersom noen krietier er sanne
                    // Eventet er ikke ekstern og brukeren er ikke en bekker
                    if not event.IsExternal && not isBekker then
                       Error "Eventet er ikke eksternt eller du er ikke autentisert"
                    // Eventet er kansellert
                    else if event.IsCancelled then
                       Error "Eventet er kansellert"
                    // Eventet er ikke åpent for registrering
                    else if event.OpenForRegistrationTime >= currentEpoch || (event.CloseRegistrationTime.IsSome && event.CloseRegistrationTime.Value < currentEpoch) then
                        Error "Eventet er ikke åpen for registering"
                    // Eventet har funnet sted
                    else if DateTime.now() > (DateTime.toCustomDateTime event.EndDate event.EndTime) then
                       Error "Eventet tok sted i fortiden"
                    // Eventet har ikke nok ledig plass plass
                    else if event.MaxParticipants.IsSome && numberOfParticipants = event.MaxParticipants.Value && not event.HasWaitingList then
                        Error "Eventet har ikke plass"
                    else
                        let insert =    
                            try
                                let participant = Queries.addParticipantToEvent eventId email userId writeModel.Name transaction
                                let answers =
                                    if List.isEmpty writeModel.ParticipantAnswers then
                                        Ok []
                                    else
                                        // FIXME: Here we need to fetch all the questions from the database. This is because we have no question ID related to the answers. This does not feel right and should be fixed.
                                        let eventQuestions = V2.Queries.getEventQuestions eventId transaction
                                        let participantAnswerDbModels: ParticipantAnswerDbModel list =
                                            writeModel.ParticipantAnswers
                                            |> List.zip eventQuestions
                                            |> List.map (fun (question, answer) -> 
                                                { QuestionId = question.Id
                                                  EventId = eventId
                                                  Email = email
                                                  Answer = answer
                                                })
                                        Queries.createParticipantAnswers participantAnswerDbModels transaction
                                        
                                match participant, answers with
                                | Ok participant, Ok answers ->
                                    let answers = List.map (fun answer -> answer.Answer) answers
                                    Ok (participant, answers)
                                | Error e1, Error e2 ->
                                   $"""Feil med lagring av deltaker og spørsmål.
                                   Deltaker: {e1}.
                                   Spørsmål: {e2}."""
                                   |> Error
                                | Error e, Ok _ ->
                                    Error $"Feil med lagring av deltaker: {e}"
                                | Ok _, Error e ->
                                    Error $"Feil med lagring av deltakerspørsmål: {e}"
                            with
                            | ex ->
                                transaction.Rollback()
                                Error ex.Message
                                
                        Result.map (fun (participant: DbModel, answers) ->
                            transaction.Commit()
                            DomainModels.Participant.CreateFromPrimitives participant.Name participant.Email answers participant.EventId participant.RegistrationTime participant.CancellationToken participant.EmployeeId
                            ) insert
                | Error e, _ -> Error e
                | _, Error e -> Error $"Fikk ikke til å parse request body: {e}"
                
            return!
                match result with
                | Ok result ->
                    let newlyCreatedParticipantViewModel = Models.domainToViewWithCancelInfo result
                    json newlyCreatedParticipantViewModel next context
                | Error e ->
                    context.SetStatusCode 400
                    json [InternalErrorMessage e] next context
        }