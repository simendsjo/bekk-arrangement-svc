namespace ArrangementService

open ArrangementService
open ArrangementService.Email
open ResultComputationExpression
open ArrangementService.DomainModels
open DateTime
open CalendarInvite
open ArrangementService.Event
open Microsoft.AspNetCore.Http
open System

module Service =

    // Arrangement markert som IsHidden skal bare være synlig
    // om man er arrangør eller deltaker (eller er admin? nei)
    // Unntak er om man har direktelink seff, men det er ikke relevant for denne funksjonen
    let keepOnlyVisibleEvents (events: Event seq) =
        result {
            let! userId = Auth.getUserId
            // let! isAdmin = Auth.isAdmin
            //                 >> Task.map (function
            //                 | Ok () -> Ok true
            //                 | Error _ -> Ok false)

            let! participationsForUser =
                Participant.Queries.queryParticipationsByEmployeeId (userId |> Option.defaultValue -1 |> EmployeeId)

            let hasPermissionToView (event: Event) =
                not event.IsHidden
                || Some event.OrganizerId.Unwrap = userId
                || (participationsForUser |> Seq.map (fun x -> x.EventId) |> Seq.contains event.Id)
                // || isAdmin

            return events
                |> Seq.filter hasPermissionToView
        }

    let getEvents: Handler<Event seq> =
        result {
            let! events = Event.Queries.getEvents 
            let! visibleEvents = events |> keepOnlyVisibleEvents
            return visibleEvents
        }
    
    let getPastEvents: Handler<Event seq> =
        result {
            let! events = Event.Queries.getPastEvents
            let! visibleEvents = events |> keepOnlyVisibleEvents
            return visibleEvents
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! eventsByOrganizer = Event.Queries.queryEventsOrganizedByEmail organizerEmail 
            return eventsByOrganizer
        }

    let getEventsOrganizedByOrganizerId (organizerId:Event.EmployeeId) =
        result {
            let! eventsByOrganizer = Event.Queries.queryEventsOrganizedByOrganizerId organizerId
            return eventsByOrganizer
        }

    let getEvent id =
        result {
            let! event = Event.Queries.queryEventByEventId id
            return event
        }
        
(*
    
       let registerParticipationv2 guid email context =
        let query =
            "
            IF EXISTS(select Id
                      FROM Events as e
                               LEFT JOIN (SELECT EventId, count(*) as numParticipants from Participants group by EventId) as part
                                         on part.EventId = e.Id
                      WHERE e.Id = (@EventId)
                        AND e.IsExternal = 1
                        AND e.OpenForRegistrationTime < (@currentEpoch)
                        AND (e.CloseRegistrationTime is null OR e.CloseRegistrationTime > (@currentEpoch))
                        AND (e.MaxParticipants is null OR ((e.HasWaitingList = 0) AND part.numParticipants < e.MaxParticipants)))
                select 1
                --insert into Participants
                --VALUES ('mats.jonassen@bekk.no', '41be0c13-2369-493b-a83e-4e6558ae676a', 1644404400000,
                --        '17996ddc-d78b-482e-92eb-3a2b76c4304e', 'Mats Jonassen', null)
            "
            
        printfn "Register participation queries.fs" 
        let foobar = Database.runCreateParticipationCommand guid query context
        printfn "Foobar"
        
            // FOR NOW
    let runCreateParticipationCommand (eventId: System.Guid) sql (context: HttpContext) =
        printfn "Hello from Database layer"
        printfn "Hello from result in DB layer"
//        let! config = getConfig >> Ok >> Task.wrap
        printfn "Getting connection"
        let config = context.GetService<AppConfig>()
        let connection = new SqlConnection(config.databaseConnectionString)
        let transaction = connection.BeginTransaction()
        printfn "Connection"
        printfn "Creating command"
        let command = new SqlCommand(sql, connection)
        printfn "Current epoch"
        let currentEpoch = System.DateTimeOffset.Now.ToUnixTimeMilliseconds()
        printfn "Adding valies..."
        command.Parameters.AddWithValue("@EventId", eventId) |> ignore
        command.Parameters.AddWithValue("@currentEpoch", currentEpoch) |> ignore
        printfn "Running query"
        let foo = command.ExecuteNonQuery ()
        printfn $"Foo: {foo}"
        transaction.Commit() 
    
    *)


    let private createdEventMessage (viewUrl: string option) createEditUrl (event: Event) =
        [ $"Hei {event.OrganizerName.Unwrap}! 😄"
          $"Arrangementet ditt {event.Title.Unwrap} er nå opprettet." 
          match viewUrl with
          | None -> ""
          | Some url -> $"Se arrangmentet, få oversikt over påmeldte deltagere og gjør eventuelle endringer her: {url}."
          $"Her er en unik lenke for å endre arrangementet: {createEditUrl event}."
          "Del denne kun med personer som du ønsker skal ha redigeringstilgang.🕵️" ]
        |> String.concat "<br>"

    let private organizerAsParticipant (event: Event): Participant =
        {
          Name = Participant.Name event.OrganizerName.Unwrap
          Email = EmailAddress event.OrganizerEmail.Unwrap
          ParticipantAnswers = Participant.ParticipantAnswers []
          EventId = event.Id
          RegistrationTime = TimeStamp.now()
          CancellationToken = System.Guid.Empty
          EmployeeId = Participant.EmployeeId (Some event.OrganizerId.Unwrap)
        }

    let private createEmail viewUrl createEditUrl noReplyMail (event: Event) =
        let message = createdEventMessage viewUrl createEditUrl event
        { Subject = $"Du opprettet {event.Title.Unwrap}"
          Message = message
          To = event.OrganizerEmail
          CalendarInvite = 
              createCalendarAttachment
                  (event, organizerAsParticipant event, noReplyMail, message, Create) |> Some 
        }

    let private sendNewlyCreatedEventMail viewUrl createEditUrl (event: Event) (ctx: HttpContext) =
        let config = Config.getConfig ctx
        let mail =
            createEmail viewUrl createEditUrl (EmailAddress config.noReplyEmail) event
        Service.sendMail mail ctx

    type EventWithShortname =
        | EventExistsWithShortname of Event
        | UnusedShortname

    let private setShortname eventId shortname =
        result {
            let! shortnameExists =
                    Queries.queryEventByShortname shortname
                    >> Task.map (function
                    | Ok event -> EventExistsWithShortname event |> Ok
                    | Error _ -> UnusedShortname |> Ok)

            match shortnameExists with
            | UnusedShortname ->
                yield! result.Zero()

            | EventExistsWithShortname event ->
                if event.EndDate >= DateTime.now() then
                    return! Error [ UserMessages.shortnameIsInUse shortname ] |> Task.wrap
                else
                    yield! Queries.deleteShortname shortname

            yield! Queries.insertShortname eventId shortname
        }

    let createEvent viewUrl createEditUrl employeeId event =
        result {
            let! newEvent = Queries.createEvent employeeId event

            match event.ParticipantQuestions with
            | [] -> 
                yield! result.Zero()
            | questions ->
                yield! Queries.insertQuestions newEvent.Id questions

            match event.Shortname with
            | None -> 
                yield! result.Zero()
            | Some shortname ->
                yield! setShortname newEvent.Id shortname

            yield sendNewlyCreatedEventMail viewUrl createEditUrl newEvent

            return newEvent
        }

    let cancelEvent event =
        result {
            do! Event.Queries.updateEvent { event with IsCancelled = true }
            return Event.UserMessages.eventSuccessfullyCancelled event.Title
        }
    
    let deleteEvent id =
        result {
            do! Event.Queries.deleteEvent id
            return Event.UserMessages.eventSuccessfullyDeleted id
        }

    let private inviteMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          ""
          $"Du er nå påmeldt {event.Title.Unwrap}."
          $"Vi gleder oss til å se deg på {event.Location.Unwrap} den {toReadableString event.StartDate} 🎉"
          ""
          if event.MaxParticipants.Unwrap.IsSome then 
            "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger<br>kan delta. Da blir det plass til andre på ventelisten 😊"
          else "Gjerne meld deg av dersom du ikke lenger har mulighet til å delta."
          $"Du kan melde deg av <a href=\"{redirectUrl}\">via denne lenken</a>."
          ""
          $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail.Unwrap}\">{event.OrganizerEmail.Unwrap}</a> om det er noe du lurer på."
          "Vi sees!"
          ""
          $"Hilsen {event.OrganizerName.Unwrap} i Bekk" ]
        |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

    let private waitlistedMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          ""
          $"Du er nå på venteliste for {event.Title.Unwrap} på {event.Location.Unwrap} den {toReadableString event.StartDate}."
          "Du vil få beskjed på e-post om du rykker opp fra ventelisten."
          ""
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          $"Du kan melde deg av <a href=\"{redirectUrl}\">via denne lenken</a>."
          "NB! Ta vare på lenken til senere - om du rykker opp fra ventelisten bruker du fortsatt denne til å melde deg av."
          ""
          $"Bare send meg en mail på <a href=\"mailto:{event.OrganizerEmail.Unwrap}\">{event.OrganizerEmail.Unwrap}</a> om det er noe du lurer på."
          "Vi sees!"
          ""
          $"Hilsen {event.OrganizerName.Unwrap } i Bekk" ]
        |> String.concat "<br>"

    let createNewParticipantMail
        createCancelUrl
        (event: Event)
        isWaitlisted
        noReplyMail
        (participant: Participant)
        =
        let message =
            if isWaitlisted
            then waitlistedMessage (createCancelUrl participant) event
            else inviteMessage (createCancelUrl participant) event

        { Subject = event.Title.Unwrap
          Message = message
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant, noReplyMail, message, Create) |> Some 
        }

    let private createCancelledParticipationMailToOrganizer
        (event: Event)
        (participant: Participant)
        =
        { Subject = "Avmelding"
          Message = $"{participant.Name.Unwrap} har meldt seg av {event.Title.Unwrap}" 
          To = event.OrganizerEmail
          CalendarInvite = None 
        }

    let private createCancelledParticipationMailToAttendee
        (event: Event)
        (participant: Participant)
        =
        { Subject = "Avmelding"
          Message = [
                    $"Vi bekrefter at du nå er avmeldt {event.Title.Unwrap}." 
                    ""
                    "Takk for at du gir beskjed! Vi håper å se deg ved en senere anledning.😊"
                    ]
                    |> String.concat "<br>"
          To = participant.Email
          CalendarInvite = None 
        }

    let private createFreeSpotAvailableMail
        (event: Event)
        (participant: Participant)
        =
        { Subject = $"Du har fått plass på {event.Title.Unwrap}!" 
          Message = $"Du har rykket opp fra ventelisten for {event.Title.Unwrap}! Hvis du ikke lenger kan delta, meld deg av med lenken fra forrige e-post."
          To = participant.Email
          CalendarInvite = None 
        }

    let private createCancelledEventMail
        (message: string)
        (event: Event)
        noReplyMail
        (participant: Participant)
        =
        { Subject = $"Avlyst: {event.Title.Unwrap}"
          Message = message.Replace("\n", "<br>")
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant, noReplyMail, message, Cancel) |> Some 
        }

    let registerParticipant createMail participant =
        result {
            do! Participant.Queries.createParticipant participant
            do! Participant.Queries.setAnswers participant

            yield Service.sendMail (createMail participant)

            return ()
        }

    let getParticipant (eventId, email: EmailAddress) =
        result {
            let! participant = Participant.Queries.queryParticipantByKey (eventId, email)

            return participant
        }

    let getParticipantsForEvent (event: Event): Handler<Participant.ParticipantsWithWaitingList> =
        result {
            let! participantsForEvent =
                Participant.Queries.queryParticipantsByEventId event.Id
            
            match event.MaxParticipants.Unwrap with
            // Max participants = None means participants = infinity 
            | None -> return {
                attendees =
                    participantsForEvent

                waitingList =
                    [] 
                }

            | Some maxParticipants -> return { 
                attendees =
                    Seq.truncate maxParticipants
                        participantsForEvent

                waitingList =
                    Seq.safeSkip maxParticipants
                        participantsForEvent }
                }

    let getParticipationsForParticipant email =
        result {
            let! participantsByMail = Participant.Queries.queryParticipantionByParticipant email 
            return participantsByMail
        }

    let getParticipationsByEmployeeId employeeId =
        result {
            let! participations = Participant.Queries.queryParticipationsByEmployeeId employeeId
            return participations
        }

    let private sendMailToFirstPersonOnWaitingList
        (event: Event)
        (waitingList: Participant seq)
        =
        let personWhoGotIt = Seq.tryHead waitingList
        match personWhoGotIt with
        | None -> 
            ignoreContext ()
        | Some participant ->
            Service.sendMail
                      (createFreeSpotAvailableMail event participant)

    let private sendMailToOrganizerAboutCancellation event participant =
        let mail = createCancelledParticipationMailToOrganizer event participant
        Service.sendMail mail

    let private sendMailWithCancellationConfirmation event participant =
        let mail = createCancelledParticipationMailToAttendee event participant
        Service.sendMail mail

    let private sendParticipantCancelMails event email =
        result {
            let! participants = getParticipantsForEvent event

            let attendingParticipant =
                participants.attendees
                |> Seq.tryFind (fun attendee -> attendee.Email = email)

            match attendingParticipant with
            | None -> 
                return ()
            | Some participant ->
                yield sendMailToOrganizerAboutCancellation event
                          participant
                yield sendMailWithCancellationConfirmation event 
                          participant
                let eventHasWaitingList = event.HasWaitingList
                if eventHasWaitingList then
                    yield sendMailToFirstPersonOnWaitingList event
                              participants.waitingList
                    return ()
        }

    let deleteParticipant (event, email) =
        result {
            yield! sendParticipantCancelMails event email
            let! participant = getParticipant (event.Id, email)

            do! Participant.Queries.deleteParticipant participant

            return Participant.UserMessages.participationSuccessfullyDeleted (event.Id, email)
        }

    let private createCancellationConfirmationToOrganizer
        (event: Event)
        (messageToParticipants: string)
        =
        { Subject = $"Avlyst: {event.Title.Unwrap}"
          Message = [
                    $"Du har avlyst arrangementet ditt {event.Title.Unwrap}." 
                    "Denne meldingen ble sendt til alle påmeldte:"
                    ""
                    messageToParticipants
                    ]
                    |> String.concat "<br>"
          To = event.OrganizerEmail
          CalendarInvite = None 
        }


    let sendCancellationMailToParticipants
        messageToParticipants
        noReplyMail
        participants
        event
        ctx
        =
        let sendMailToParticipant participant =
            Service.sendMail
                (createCancelledEventMail messageToParticipants event
                     noReplyMail participant) ctx

        Service.sendMail
                (createCancellationConfirmationToOrganizer event messageToParticipants) ctx

        participants |> Seq.iter sendMailToParticipant

        ()
    
    let getNumberOfParticipantsForEvent eventId =
        result {
            let! count = Participant.Queries.queryNumberOfParticipantsForEvent eventId
            return Event.NumberOfParticipants count
        }
    
    let getWaitinglistSpot eventId email =
        result {
            let! event = getEvent eventId

            let! { attendees = attendees
                   waitingList = waitingList } = getParticipantsForEvent event

            let isParticipant =  
                Seq.append attendees waitingList 
                |> Seq.exists (fun y -> y.Email = email)

            if not isParticipant then
                return! Error [ Participant.UserMessages.participantNotFound email ] |> Task.wrap

            else
                let waitingListIndex = 
                    waitingList 
                    |> Seq.tryFindIndex (fun participant -> participant.Email = email)
                
                return waitingListIndex 
                    |> Option.map (fun index -> index + 1) 
                    |> Option.defaultValue 0 
        }

    (*
        We want to know how many of the questions have actually been seen and answered by people
    *)
    let getNumberOfQuestionsThatHaveBeenAnswered (eventId: Event.Id) =
        result {
            let! participants = Participant.Queries.queryParticipantsByEventId eventId

            if Seq.isEmpty participants then
                return 0
            else

            return participants 
                        |> Seq.map (fun p -> p.ParticipantAnswers.Unwrap |> Seq.length) 
                        |> Seq.max
        }


    (* 
        This function fetches the event from the database so it fits with our
        writeToDomain function. Every field in the writeModel-records needs to be present when
        updating, even though you might only want to update one field. Its not a very
        intuitive solution but it works for now
        -- Summer intern 2021
    *)
    let updateEvent (id: Event.Id) writeModel =
        result {
            let! oldEvent = Event.Queries.queryEventByEventId id
            let! newEvent = Event.Models.writeToDomain id.Unwrap writeModel oldEvent.EditToken oldEvent.IsCancelled oldEvent.OrganizerId.Unwrap |> Task.wrap |> ignoreContext

            let! { waitingList = oldWaitingList } = getParticipantsForEvent oldEvent

            do! Event.Validation.assertValidCapacityChange oldEvent newEvent
            do! Event.Queries.updateEvent newEvent

            if newEvent.ParticipantQuestions <> oldEvent.ParticipantQuestions then
                let! numberOfAnsweredQuestions = getNumberOfQuestionsThatHaveBeenAnswered newEvent.Id
                let unansweredQuestions = newEvent.ParticipantQuestions.Unwrap |> Seq.safeSkip numberOfAnsweredQuestions |> List.ofSeq
                let answeredQuestions = newEvent.ParticipantQuestions.Unwrap |> Seq.truncate numberOfAnsweredQuestions |> List.ofSeq
                if answeredQuestions <> (oldEvent.ParticipantQuestions.Unwrap |> List.truncate numberOfAnsweredQuestions) then
                    return! Error [ UserMessages.illegalQuestionsUpdate ] |> Task.wrap
                else
                    yield! Queries.deleteLastQuestions ((oldEvent.ParticipantQuestions.Unwrap |> Seq.length) - (answeredQuestions |> Seq.length)) newEvent.Id
                    yield! Queries.insertQuestions newEvent.Id unansweredQuestions

            if newEvent.Shortname <> oldEvent.Shortname then
                match oldEvent.Shortname with
                | Shortname (Some oldShortname) ->
                    yield! Queries.deleteShortname oldShortname
                | Shortname None ->
                    yield! result.Zero()

                match newEvent.Shortname with
                | Shortname (Some shortname) ->
                    yield! setShortname newEvent.Id shortname
                | Shortname None ->
                    yield! result.Zero()

            let numberOfNewPeople =
                match oldEvent.MaxParticipants.Unwrap, newEvent.MaxParticipants.Unwrap with
                | Some _, None -> Seq.length oldWaitingList
                | Some old, Some new' -> new' - old
                | _ -> 0

            if numberOfNewPeople > 0 then
                let newPeople =
                    oldWaitingList
                    |> Seq.truncate numberOfNewPeople
                for newAttendee in newPeople do
                    yield Service.sendMail <| createFreeSpotAvailableMail newEvent newAttendee

            return newEvent 
        }

    let getEventByShortname shortname = 
        result {
            let! event = Event.Queries.queryEventByShortname shortname
            return event
        }
    

    let participantToCSVRow (questions: ParticipantQuestions) (participant: Participant) =

        let questions = questions.Unwrap
        let answers = participant.ParticipantAnswers.Unwrap
        let qas = Seq.zip questions answers
                        |> Seq.filter (fun (q, a) -> a <> "")

        let dobbeltfnuttTilEnkelfnutt character = if character='"' then '\'' else character
        let escapeComma word = $"\"{word}\""
        let cleaning = String.map dobbeltfnuttTilEnkelfnutt >> escapeComma

        [participant.Name.Unwrap
         participant.Email.Unwrap
         qas |> Seq.map (fun (q, a) -> $"{q}\n{a}") |> String.concat "\n\n"
        ] 
        |> List.map cleaning
        |> String.concat ","

    let createExportEventString (event:Event) (participants:Participant.ParticipantsWithWaitingList) =
        let newline="\n"

        let attendees = participants.attendees 
                        |> Seq.map (participantToCSVRow event.ParticipantQuestions)
                        |> String.concat newline 
        let waitingList = participants.waitingList 
                          |> Seq.map (participantToCSVRow event.ParticipantQuestions)
                          |> String.concat newline
        let eventName = event.Title.Unwrap 
        let attendeesTitle = "Påmeldte"
        let waitinglistTitle = "Venteliste"
        let header = ["Navn";"Epost";"Kommentar"] |> String.concat ","

        [eventName 
         attendeesTitle 
         header 
         attendees 
         waitinglistTitle
         header 
         waitingList 
        ] |> String.concat newline

    let exportParticipationsDataForEvent (id: Event.Id) =
        result {
            let! event = Event.Queries.queryEventByEventId id
            let! participants = getParticipantsForEvent event
            let str = createExportEventString event participants
            return str
        }
