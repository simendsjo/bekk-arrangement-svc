module V2.Handlers

open System
open System.Web
open Giraffe
open Microsoft.Data.SqlClient
open Microsoft.AspNetCore.Http
open Thoth.Json.Net

open ArrangementService
open UserMessage
open ArrangementService.DomainModels
open ArrangementService.Event
open ArrangementService.Participant

type private ParticipateEvent =
    | NotExternal 
    | IsCancelled
    | NotOpenForRegistration
    | HasAlreadyTakenPlace
    | NoRoom
    | IsWaitListed
    | CanParticipate
    
let private participateEvent isBekker numberOfParticipants (event: Event.DbModel) =
    let currentEpoch = DateTimeOffset.Now.ToUnixTimeMilliseconds()
    let hasRoom = event.MaxParticipants.IsNone || event.MaxParticipants.IsSome && numberOfParticipants < event.MaxParticipants.Value
    // Eventet er ikke ekstern 
    // Brukeren er ikke en bekker
    if not event.IsExternal && not isBekker then
        NotExternal
    // Eventet er kansellert
    else if event.IsCancelled then
        IsCancelled
    // Eventet er ikke åpent for registrering
    else if event.OpenForRegistrationTime >= currentEpoch || (event.CloseRegistrationTime.IsSome && event.CloseRegistrationTime.Value < currentEpoch) then
        NotOpenForRegistration
    // Eventet har funnet sted
    else if DateTime.now() > (DateTime.toCustomDateTime event.EndDate event.EndTime) then
        HasAlreadyTakenPlace
    // Eventet har ikke nok ledig plass plass
    else if not hasRoom && not event.HasWaitingList then
        NoRoom
    else if hasRoom && event.HasWaitingList then
        IsWaitListed
    else
        CanParticipate
        
let registerParticipationHandler (eventId: Guid, email): HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let isBekker = context.User.Identity.IsAuthenticated
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
                    let canParticipate = 
                        match participateEvent isBekker numberOfParticipants event with
                            | NotExternal ->
                                Error "Eventet er ikke eksternt"
                            | IsCancelled ->
                                Error "Eventet er kansellert"
                            | NotOpenForRegistration ->
                                Error "Eventet er ikke åpen for registering"
                            | HasAlreadyTakenPlace ->
                                Error "Eventet tok sted i fortiden"
                            | NoRoom ->
                                Error "Eventet har ikke plass"
                            | IsWaitListed ->
                                Ok IsWaitListed
                            | CanParticipate ->
                                Ok CanParticipate
                    match canParticipate with
                    | Error e -> Error e
                    | Ok participate -> 
                        let insert =    
                            try
                                let participant = Queries.addParticipantToEvent eventId email userId writeModel.Name transaction
                                let answers =
                                    if List.isEmpty writeModel.ParticipantAnswers then
                                        Ok []
                                    else
                                        // FIXME: Here we need to fetch all the questions from the database. This is because we have no question ID related to the answers. This does not feel right and should be fixed. Does require a frontend fix as well
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
                                   $"""Feil med lagring av deltaker og svar.
                                   Deltaker: {e1}.
                                   Spørsmål: {e2}."""
                                   |> Error
                                | Error e, Ok _ ->
                                    Error $"Feil med lagring av deltaker: {e}"
                                | Ok _, Error e ->
                                    Error $"Feil med lagring av deltakersvar: {e}"
                            with
                            | ex ->
                                transaction.Rollback()
                                Error ex.Message
                                
                        Result.map (fun (participant: DbModel, answers) ->
                            transaction.Commit()
                            // FIXME: we need these domain models as the rest of the system all work with these
                            // Lage domenemodell av participant
                            let participantDomainModel = DomainModels.Participant.CreateFromPrimitives participant.Name participant.Email answers participant.EventId participant.RegistrationTime participant.CancellationToken participant.EmployeeId
                            // Lag domenemodell av event
                            let eventDomainModel = Event.Models.dbToDomain(event, [], None)
                            // Sende epost
                            let isWaitlisted = participate = IsWaitListed
                            let email =
                                let redirectUrlTemplate =
                                    HttpUtility.UrlDecode writeModel.CancelUrlTemplate

                                let createCancelUrl (participant: Participant) =
                                    redirectUrlTemplate.Replace("{eventId}",
                                                                participant.EventId.Unwrap.ToString
                                                                    ())
                                                       .Replace("{email}",
                                                                participant.Email.Unwrap
                                                                |> Uri.EscapeDataString)
                                                       .Replace("{cancellationToken}",
                                                                participant.CancellationToken.ToString
                                                                    ())
                                
                                Service.createNewParticipantMail
                                    createCancelUrl eventDomainModel isWaitlisted
                                    (Email.EmailAddress config.noReplyEmail)
                                    participantDomainModel
                            ArrangementService.Email.Service.sendMail email context
                            
                            participantDomainModel
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
                    convertUserMessagesToHttpError [BadInput e] next context
        }