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

    let setQuestions (eventId: Event.Id) questions =
        result {
            do! insert { table questionsTable
                         values (questions |> List.map (fun question -> {| EventId = eventId.Unwrap; Question = question |}))
                       } 
                       |> flip Database.runInsertQuery
        }

    let private groupEventAndShortname ls =
        ls
        |> Seq.map (fun (event: DbModel, questionDbModel: ParticipantQuestionDbModel option, shortnameDbModel: ShortnameDbModel option) ->
                        ( event
                        , questionDbModel 
                        , shortnameDbModel 
                            |> Option.map (fun dbModel -> dbModel.Shortname)))
        |> Seq.groupBy (fun (event, _, _) -> event.Id)
        |> Seq.map (fun (eventId, listOfQuestions) -> 
            let event = listOfQuestions |> Seq.head |> fun (event, _, _) -> event
            let shortname = listOfQuestions |> Seq.head |> fun (_, _, shortname) -> shortname
            ( event
            , listOfQuestions 
                |> Seq.collect (fun (_, question, _) -> match question with | Some q -> [ q ] | None -> []) 
                |> Seq.sortBy (fun q -> q.Id) 
                |> Seq.map (fun q -> q.Question) 
                |> List.ofSeq
            , shortname
            ))

    let getEvents (ctx: HttpContext): Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Events.Id"
                 leftJoin shortnamesTable "EventId" "Events.Id"
                 where (ge "EndDate" DateTime.Now)
                 orderBy "StartDate" Asc }
        |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel> ctx
        |> groupEventAndShortname
        |> Seq.map Models.dbToDomain

    let getPastEvents (ctx: HttpContext): Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Events.Id"
                 leftJoin shortnamesTable "EventId" "Events.Id"
                 where (lt "EndDate" DateTime.Now + eq "IsCancelled" false)
                 orderBy "StartDate" Desc }
        |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
        |> groupEventAndShortname
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
                 leftJoin questionsTable "EventId" "Events.Id"
                 leftJoin shortnamesTable "EventId" "Events.Id"
                 where (eq "Events.Id" eventId.Unwrap)
               }
       |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
       |> groupEventAndShortname
       |> Seq.tryHead
       |> function
       | Some eventWithShortname -> Ok <| Models.dbToDomain eventWithShortname
       | None -> Error [ UserMessages.eventNotFound eventId ]

    let queryEventsOrganizedByEmail (organizerEmail: EmailAddress) ctx: Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Events.Id"
                 leftJoin shortnamesTable "EventId" "Events.Id" 
                 where (eq "Email" organizerEmail.Unwrap)
               }
       |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
       |> groupEventAndShortname
       |> Seq.map Models.dbToDomain

    let queryEventsOrganizedByOrganizerId (organizerId: EmployeeId) ctx: Event seq =
        select { table eventsTable
                 leftJoin questionsTable "EventId" "Events.Id"
                 leftJoin shortnamesTable "EventId" "Events.Id" 
                 where (eq "OrganizerId" organizerId.Unwrap)
               }
       |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>  ctx
       |> groupEventAndShortname
       |> Seq.map Models.dbToDomain

    let queryEventByShortname (shortname: string) ctx: Result<Event, UserMessage list> =
        select { table eventsTable 
                 leftJoin shortnamesTable "EventId" "Events.Id"
                 leftJoin questionsTable "EventId" "Events.Id"
                 where (eq "Shortname" shortname)
               }
       |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ShortnameDbModel, ParticipantQuestionDbModel>  ctx
       |> Seq.map (fun (event, shortname, question) -> (event, question, shortname))
       |> groupEventAndShortname
       |> Seq.tryHead
       |> function
       | Some eventWithShortname -> Ok <| Models.dbToDomain eventWithShortname
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
