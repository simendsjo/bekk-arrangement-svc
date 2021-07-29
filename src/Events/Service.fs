namespace ArrangementService

open ArrangementService
open ArrangementService.Email
open ResultComputationExpression
open ArrangementService.DomainModels
open Http
open DateTime
open CalendarInvite
open ArrangementService.Event

module Service =

    let getEvents: Handler<Event seq> =
        result {
            let! events = Event.Queries.getEvents >> Ok
            return events
        }
    
    let getPastEvents: Handler<Event seq> =
        result {
            let! events = Event.Queries.getPastEvents >> Ok
            return events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            let! eventsByOrganizer = Event.Queries.queryEventsOrganizedByEmail organizerEmail >> Ok
            return eventsByOrganizer
        }
    let getEventsOrganizedByOrganizerId (organizerId:Event.EmployeeId) =
        result {
            let! eventsByOrganizer = Event.Queries.queryEventsOrganizedByOrganizerId organizerId >> Ok
            return eventsByOrganizer
        }

    let getEvent id =
        result {
            let! event = Event.Queries.queryEventByEventId id
            return event
        }

    let private createdEventMessage (viewUrl: string option) createEditUrl (event: Event) =
        [ $"Hei {event.OrganizerName.Unwrap}! 😄"
          $"Arrangementet ditt {event.Title.Unwrap} er nå opprettet" 
          $"Se arrangmentet, få oversikt over påmeldte deltagere og gjør eventuelle endringer her:" + (match viewUrl with
                                                                                                      | None -> "."
                                                                                                      | Some url -> $": {url}.")
          $"Her er en unik lenke for å endre arrangementet: {createEditUrl event}."
          "Del denne kun med personer som du ønsker skal ha redigeringstilgang.🕵️" ]
        |> String.concat "<br>"

    let private createEmail viewUrl createEditUrl (event: Event) =
        let message = createdEventMessage viewUrl createEditUrl event
        { Subject = $"Du opprettet {event.Title.Unwrap}"
          Message = message
          To = event.OrganizerEmail
          CalendarInvite = None }

    let private sendNewlyCreatedEventMail viewUrl createEditUrl (event: Event) =
        result {
            let mail =
                createEmail viewUrl createEditUrl event
            yield Service.sendMail mail
        }

    type EventWithShortname =
        | EventExistsWithShortname of Event
        | UnusedShortname

    let private setShortname eventId shortname =
        result {
            let! shortnameExists =
                    Queries.queryEventByShortname shortname
                    >> function
                    | Ok event -> EventExistsWithShortname event |> Ok
                    | Error _ -> UnusedShortname |> Ok

            match shortnameExists with
            | UnusedShortname ->
                yield! Ok () |> ignoreContext

            | EventExistsWithShortname event ->
                if event.EndDate >= DateTime.now() then
                    return! Error [ UserMessages.shortnameIsInUse shortname ]
                else
                    yield! Queries.deleteShortname shortname

            yield! Queries.insertShortname eventId shortname
            return ()
        }

    let createEvent viewUrl createEditUrl employeeId event =
        result {
            let! newEvent = Queries.createEvent employeeId event

            match event.Shortname with
            | None -> yield! Ok () |> ignoreContext
            | Some shortname ->
                yield! setShortname newEvent.Id shortname

            yield sendNewlyCreatedEventMail viewUrl createEditUrl newEvent

            return newEvent
        }

    let cancelEvent event =
        result {
            do! Event.Queries.updateEvent {event with IsCancelled=true}
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
          To = event.OrganizerEmail
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
                >> Ok
            
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
            
            let! participantsByMail = Participant.Queries.queryParticipantionByParticipant email >> Ok

            return participantsByMail
        }
    let getParticipationsByEmployeeId employeeId =
        result {
            let! participations = Participant.Queries.queryParticipationsByEmployeeId employeeId >> Ok
            return participations
        }

    let private sendMailToFirstPersonOnWaitingList
        (event: Event)
        (waitingList: Participant seq)
        =
        result {
            let personWhoGotIt = Seq.tryHead waitingList
            match personWhoGotIt with
            | None -> return ()
            | Some participant ->
                yield Service.sendMail
                          (createFreeSpotAvailableMail event participant)
        }

    let private sendMailToOrganizerAboutCancellation event participant =
        result {
            let mail = createCancelledParticipationMailToOrganizer event participant

            yield Service.sendMail mail
        }

    let private sendMailWithCancellationConfirmation event participant =
        result {
            let mail = createCancelledParticipationMailToAttendee event participant

            yield Service.sendMail mail
        }

    let private sendParticipantCancelMails event email =
        result {
            let! participants = getParticipantsForEvent event

            let attendingParticipant =
                participants.attendees
                |> Seq.tryFind (fun attendee -> attendee.Email = email)

            match attendingParticipant with
            | None -> return ()
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
            yield sendParticipantCancelMails event email
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
                (createCancellationConfirmationToOrganizer event messageToParticipants)

        participants |> Seq.iter sendMailToParticipant

        Ok()
    
    let getNumberOfParticipantsForEvent eventId =
        result {
            let! count = Participant.Queries.getNumberOfParticipantsForEvent eventId
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
                return! Error [ Participant.UserMessages.participantNotFound email ]

            else
                let waitingListIndex = 
                    waitingList 
                    |> Seq.tryFindIndex (fun participant -> participant.Email = email)
                
                return waitingListIndex 
                    |> Option.map (fun index -> index + 1) 
                    |> Option.defaultValue 0 
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
            let! newEvent = Event.Models.writeToDomain id.Unwrap writeModel oldEvent.EditToken oldEvent.IsCancelled oldEvent.OrganizerId.Unwrap |> ignoreContext

            let! { waitingList = oldWaitingList } = getParticipantsForEvent oldEvent

            do! Event.Validation.assertValidCapacityChange oldEvent newEvent
            do! Event.Queries.updateEvent newEvent

            if newEvent.Shortname <> oldEvent.Shortname then
                match oldEvent.Shortname with
                | Shortname (Some oldShortname) ->
                    yield! Queries.deleteShortname oldShortname
                | Shortname None ->
                    yield! Ok () |> ignoreContext

                match newEvent.Shortname with
                | Shortname (Some shortname) ->
                    yield! setShortname newEvent.Id shortname
                | Shortname None ->
                    yield! Ok () |> ignoreContext

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
    

    let participantToCSVRow (participant:Participant) =
        let dobbeltfnuttTilEnkelfnutt character = if character='"' then '\'' else character
        let escapeComma word = $"\"{word}\""
        let cleaning = String.map dobbeltfnuttTilEnkelfnutt >> escapeComma

        [participant.Name.Unwrap
         participant.Email.Unwrap
         participant.Comment.Unwrap] 
        |> List.map cleaning
        |> String.concat ","

    let createExportEventString (event:Event) (participants:Participant.ParticipantsWithWaitingList) =
        let newline="\n"

        let attendees = participants.attendees 
                        |> Seq.map participantToCSVRow 
                        |> String.concat newline 
        let waitingList = participants.waitingList 
                          |> Seq.map participantToCSVRow 
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
