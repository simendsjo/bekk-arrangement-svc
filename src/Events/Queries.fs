namespace ArrangementService.Event

open System
open System.Data.SqlClient
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
open System.Threading.Tasks

module Queries =

    let eventsTable = "Events"
    let questionsTable = "ParticipantQuestions"
    let shortnamesTable = "Shortnames"

    let createEvent employeeId (event: WriteModel)  =
        result {
            let! event = Models.writeToDomain (Guid.NewGuid()) event (Guid.NewGuid()) false employeeId |> Task.wrap |> ignoreContext
            let dbModel = Models.domainToDb event
            do! insert { table eventsTable
                         value dbModel
                       }
                |> Database.runInsertQuery
            return Models.dbToDomain (dbModel, event.ParticipantQuestions.Unwrap, event.Shortname.Unwrap)
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
            let (event, _, shortname) = listOfQuestions |> Seq.head
            let sortedQuestionsForEvent =
                listOfQuestions 
                |> Seq.collect (fun (_, question, _) -> match question with | Some q -> [ q ] | None -> []) 
                |> Seq.sortBy (fun q -> q.Id) 
                |> Seq.map (fun q -> q.Question) 
                |> List.ofSeq

            (event, sortedQuestionsForEvent, shortname)
        )

    let getEvents: Handler<Event seq> =
        result {
            let! events =
                select { table eventsTable
                         leftJoin questionsTable "EventId" "Events.Id"
                         leftJoin shortnamesTable "EventId" "Events.Id"
                         where (ge "EndDate" DateTime.Now)
                         orderBy "StartDate" Asc }
                |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel> 

            let groupedEvents =
                events
                |> groupEventAndShortname

            return Seq.map Models.dbToDomain groupedEvents
        }

    let getPastEvents: Handler<Event seq> =
        result {
            let! events =
                select { table eventsTable
                         leftJoin questionsTable "EventId" "Events.Id"
                         leftJoin shortnamesTable "EventId" "Events.Id"
                         where (lt "EndDate" DateTime.Now + eq "IsCancelled" false)
                         orderBy "StartDate" Desc }
                |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>
            
            let groupedEvents =
                events
                |> groupEventAndShortname

            return Seq.map Models.dbToDomain groupedEvents
        }

    let deleteEvent (id: Event.Id): Handler<unit> =
        result {
            let! res =
                delete { table eventsTable
                         where (eq "Id" id.Unwrap)
                       }
                |> Database.runDeleteQuery
            return ()
        }

    let updateEvent (newEvent: Event): Handler<unit> =
        result {
            let newEventDb = Models.domainToDb newEvent
            let! res =
                update { table eventsTable
                         set newEventDb
                         where (eq "Id" newEvent.Id.Unwrap)
                       }
                |> Database.runUpdateQuery
            return ()
        }

    let queryEventByEventId (eventId: Event.Id): Handler<Event> =
        result {
            let! events = 
                select { table eventsTable 
                         leftJoin questionsTable "EventId" "Events.Id"
                         leftJoin shortnamesTable "EventId" "Events.Id"
                         where (eq "Events.Id" eventId.Unwrap)
                       }
               |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel> 

            let event = 
                events
                |> groupEventAndShortname
                |> Seq.tryHead

            return! 
                match event with
                | Some eventWithShortname ->
                    Models.dbToDomain eventWithShortname 
                    |> Ok |> Task.wrap
                | None -> 
                    Error [ UserMessages.eventNotFound eventId ] 
                    |> Task.wrap
        }
        
    let queryEventsOrganizedByEmail (organizerEmail: EmailAddress): Handler<Event seq> =
        result {
            let! events =
                select { table eventsTable
                         leftJoin questionsTable "EventId" "Events.Id"
                         leftJoin shortnamesTable "EventId" "Events.Id" 
                         where (eq "Email" organizerEmail.Unwrap)
                       }
               |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>

            let groupedEvents =
                events
                |> groupEventAndShortname

            return Seq.map Models.dbToDomain groupedEvents
       }

    let queryEventsOrganizedByOrganizerId (organizerId: EmployeeId): Handler<Event seq> =
        result {
            let! events =
                select { table eventsTable
                         leftJoin questionsTable "EventId" "Events.Id"
                         leftJoin shortnamesTable "EventId" "Events.Id" 
                         where (eq "OrganizerId" organizerId.Unwrap)
                       }
               |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>

            let groupedEvents =
                events
                |> groupEventAndShortname

            return Seq.map Models.dbToDomain groupedEvents
        }

    let queryEventByShortname (shortname: string): Handler<Event> =
        result {
            let! events =
                select { table eventsTable 
                         leftJoin questionsTable "EventId" "Events.Id"
                         leftJoin shortnamesTable "EventId" "Events.Id"
                         where (eq "Shortname" shortname)
                       }
               |> Database.runOuterJoinJoinSelectQuery<Event.DbModel, ParticipantQuestionDbModel, ShortnameDbModel>

            let event = 
                events
                |> groupEventAndShortname
                |> Seq.tryHead

            return!
                match event with
                | Some eventWithShortname ->
                    Models.dbToDomain eventWithShortname
                    |> Ok
                    |> Task.wrap
                | None -> 
                    Error [ UserMessages.eventNotFound shortname ]
                    |> Task.wrap
        }

    let insertShortname (eventId: Event.Id) (shortname: string): Handler<unit> =
        result {
            let! () =
                queryEventByShortname shortname
                >> Task.map (function
                                | Ok _ -> Error [ UserMessages.shortnameIsInUse shortname ]
                                | Error _ -> Ok ())
            let! res =
                insert { table shortnamesTable
                         value {| Shortname = shortname; EventId = eventId.Unwrap |}
                       }
                |> Database.runInsertQuery

            return ()
        }

    let deleteShortname (shortname: string): Handler<unit> =
        result {
            let! res =
                delete { table shortnamesTable
                         where (eq "Shortname" shortname)
                       }
                |> Database.runDeleteQuery

            return ()
        }

    let getQuestionsForEvent (eventId: Event.Id) =
        result {
            let! questions = 
                select {
                    table questionsTable
                    where (eq "EventId" eventId.Unwrap)
                    orderBy "Id" Asc
                }
                |> Database.runSelectQuery<ParticipantQuestionDbModel>
            return questions
        }

    let insertQuestions (eventId: Event.Id) questions =
        result {
            if Seq.isEmpty questions then
                return ()
            else

            do! insert { table questionsTable
                         values (questions 
                                |> List.map (fun question -> 
                                    {| EventId = eventId.Unwrap; Question = question |}))
                       } 
                       |> Database.runInsertQuery
        }

    let deleteAllQuestions (eventId: Event.Id) =
        result {
            let! res = 
                delete { table questionsTable
                         where (eq "EventId" eventId.Unwrap)
                       }
                       |> Database.runDeleteQuery
            return ()
        }

    let deleteLastQuestions n (eventId: Event.Id) =
        result {
            if n <= 0 then
                return ()
            else

            let! questions = getQuestionsForEvent eventId
            let lastNQuestions = questions |> Seq.rev |> Seq.truncate n |> List.ofSeq
            let! res =
                delete { table questionsTable
                         where (isIn "Id" (lastNQuestions |> List.map (fun q -> q.Id :> obj)))
                       }
                       |> Database.runDeleteQuery
            return ()
        }