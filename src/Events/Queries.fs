namespace ArrangementService.Event

open System
open ArrangementService.Participant
open Dapper.FSharp
open Microsoft.AspNetCore.Http

open ArrangementService
open ArrangementService.DomainModels
open ArrangementService.UserMessage
open ArrangementService.Event
open ArrangementService.Email
open ArrangementService.ResultComputationExpression
open ArrangementService.Tools

module Queries =

    let eventsTable = "Events"
    let questionsTable = "ParticipantQuestions"
    let shortnamesTable = "Shortnames"

    let createEvent employeeId (event: WriteModel)  =
        // TODO: Legg inn questions i egen tabell når eventet blir oppretta
        result {
            let! event = Models.writeToDomain (Guid.NewGuid()) event (Guid.NewGuid()) false employeeId |> ignoreContext
            let dbModel = Models.domainToDb event
            do! insert { table eventsTable
                         value dbModel
                       }
                |> flip Database.runInsertQuery
            return Models.dbToDomain (dbModel, event.ParticipantQuestions.Unwrap, event.Shortname.Unwrap)
        }

    let getEvents (ctx: HttpContext): Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Events.Id"
                 leftJoin shortnamesTable "EventId" "Events.Id"
                 where (ge "EndDate" DateTime.Now)
                 orderBy "StartDate" Asc }
        |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel> ctx
        |> Seq.map (fun (event, questionDbModel, shortnameDbModel) ->
                        ( event
                        , questionDbModel 
                            |> Option.map (fun dbModel -> dbModel.Question)
                        , shortnameDbModel 
                            |> Option.map (fun dbModel -> dbModel.Shortname)))
        |> Seq.groupBy (fun (event, _, _) -> event.Id)
        |> Seq.map (fun (eventId, listOfStuff) -> 
            let event = listOfStuff |> Seq.head |> fun (event, _, _) -> event
            let shortname = listOfStuff |> Seq.head |> fun (_, _, shortname) -> shortname
            ( event
            , listOfStuff |> Seq.collect (fun (_, question, _) -> match question with | Some q -> [ q ] | None -> []) |> List.ofSeq
            , shortname
            ))
        |> Seq.map Models.dbToDomain

    let getPastEvents (ctx: HttpContext): Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Id"
                 leftJoin shortnamesTable "EventId" "Id"
                 where (lt "EndDate" DateTime.Now + eq "IsCancelled" false)
                 orderBy "StartDate" Desc }
        |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
        // TODO: wtf gjør man med question / questions her, groupe på event??
        |> Seq.map (fun (event, question, shortnameDbModel) -> (event, [], shortnameDbModel |> Option.map (fun dbModel -> dbModel.Shortname)))
        |> Seq.map Models.dbToDomain

    let deleteEvent (id: Event.Id) (ctx: HttpContext): Result<Unit, UserMessage list> =
        // TODO: cascade delete på questions her?
        // obs kva med Shortname
        delete { table eventsTable
                 where (eq "Id" id.Unwrap)
               }
        |> Database.runDeleteQuery ctx
        |> ignore
        Ok ()

    let updateEvent (newEvent: Event) (ctx: HttpContext): Result<Unit, UserMessage list> =
        // TODO: Her må det potensielt oppdateres spørsmål
        // eeellleer, kan vere det burde gjøres oppe i servicen?
        let newEventDb = Models.domainToDb newEvent
        update { table eventsTable
                 set newEventDb
                 where (eq "Id" newEvent.Id.Unwrap)
               }
        |> Database.runUpdateQuery ctx
        |> ignore
        Ok ()

    let queryEventByEventId (eventId: Event.Id) ctx: Result<Event, UserMessage list> =
        select { table eventsTable 
                 leftJoin questionsTable "EventId" "Id"
                 leftJoin shortnamesTable "EventId" "Id"
                 where (eq "Id" eventId.Unwrap)
               }
       |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
       // TODO: wtf gjør man med question / questions her, groupe på event??
       |> Seq.map (fun (event, question, shortnameDbModel) -> (event, [], shortnameDbModel |> Option.map (fun dbModel -> dbModel.Shortname)))
       |> Seq.tryHead
       |> function
       | Some eventWithShortname -> Ok <| Models.dbToDomain eventWithShortname
       | None -> Error [ UserMessages.eventNotFound eventId ]

    let queryEventsOrganizedByEmail (organizerEmail: EmailAddress) ctx: Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Id"
                 leftJoin shortnamesTable "EventId" "Id" 
                 where (eq "Email" organizerEmail.Unwrap)
               }
       |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
       // TODO: wtf gjør man med question / questions her, groupe på event??
       |> Seq.map (fun (event, question, shortnameDbModel) -> (event, [], shortnameDbModel |> Option.map (fun dbModel -> dbModel.Shortname)))
       |> Seq.map Models.dbToDomain

    let queryEventsOrganizedByOrganizerId (organizerId: EmployeeId) ctx: Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Id"
                 leftJoin shortnamesTable "EventId" "Id" 
                 where (eq "OrganizerId" organizerId.Unwrap)
               }
       |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
       // TODO: wtf gjør man med question / questions her, groupe på event??
       |> Seq.map (fun (event, question, shortnameDbModel) -> (event, [], shortnameDbModel |> Option.map (fun dbModel -> dbModel.Shortname)))
       |> Seq.map Models.dbToDomain

    let queryEventByShortname (shortname: string) ctx: Result<Event, UserMessage list> =
        select { table shortnamesTable 
                 innerJoin questionsTable "EventId" "Id"
                 where (eq "Shortname" shortname)
                 innerJoin eventsTable "Id" "EventId"
               }
       |> Database.runInnerJoinJoinSelectQuery<ShortnameDbModel, ParticipantQuestionDbModel, Event.DbModel>  ctx
       |> Seq.tryHead
       |> function
       // TODO: wtf gjør man med question / questions her, groupe på event??
       | Some (shortname, question, event) -> Ok <| Models.dbToDomain (event, [], Some shortname.Shortname)
       | None -> Error [ UserMessages.eventNotFound shortname ]

    let insertShortname (eventId: Event.Id) (shortname: string) (ctx: HttpContext): Result<Unit, UserMessage list> =
        try
            insert { table shortnamesTable
                     value {| Shortname = shortname; EventId = eventId.Unwrap |}
                   }
            |> Database.runInsertQuery ctx
            |> ignore
            Ok ()

        // Inserten kan feile feks dersom Shortname (PK) allerede finnes
        with _ -> 
            Error []

    let deleteShortname (shortname: string) (ctx: HttpContext): Result<Unit, UserMessage list> =
        delete { table shortnamesTable
                 where (eq "Shortname" shortname)
               }
        |> Database.runDeleteQuery ctx
        |> ignore
        Ok ()
