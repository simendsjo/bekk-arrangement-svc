namespace ArrangementService.Participant

open ArrangementService

open ResultComputationExpression
open ArrangementService.Email
open CalendarInvite
open Queries
open UserMessages
open Models
open ArrangementService.DomainModels
open DateTime
open Http

module Service =

    let repo = Repo.from models

    let private inviteMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          ""
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          ""
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Du kan melde deg av <a href=\"%s\">via denne lenken</a>."
              redirectUrl
          ""
          sprintf
              "Bare send meg en mail på <a href=\"mailto:%s\">%s</a> om det er noe du lurer på."
              event.OrganizerEmail.Unwrap event.OrganizerEmail.Unwrap
          "Vi sees!"
          ""
          sprintf "Hilsen %s i Bekk" event.OrganizerName.Unwrap ]
        |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

    let private waitlistedMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          ""
          sprintf "Du er nå på venteliste for %s på %s den %s."
              event.Title.Unwrap event.Location.Unwrap
              (toReadableString event.StartDate)
          "Du vil få beskjed på e-post om du rykker opp fra ventelisten."
          ""
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Du kan melde deg av <a href=\"%s\">via denne lenken</a>."
              redirectUrl
          "NB! Ta vare på lenken til senere - om du rykker opp fra ventelisten bruker du fortsatt denne til å melde deg av."
          ""
          sprintf
              "Bare send meg en mail på <a href=\"mailto:%s\">%s</a> om det er noe du lurer på."
              event.OrganizerEmail.Unwrap event.OrganizerEmail.Unwrap
          "Vi sees!"
          ""
          sprintf "Hilsen %s i Bekk" event.OrganizerName.Unwrap ]
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
                  (event, participant, noReplyMail, message, Create) |> Some }

    let private createCancelledParticipationMail
        (event: Event)
        (participant: Participant)
        =
        { Subject = "Avmelding"
          Message =
              sprintf "%s har meldt seg av %s" participant.Name.Unwrap
                  event.Title.Unwrap
          To = event.OrganizerEmail
          CalendarInvite = None }

    let private createFreeSpotAvailableMail
        (event: Event)
        (participant: Participant)
        =
        { Subject = sprintf "Du har fått plass på %s!" event.Title.Unwrap
          Message =
              sprintf
                  "Du har rykket opp fra ventelisten for %s! Hvis du ikke lenger kan delta, meld deg av med lenken fra forrige e-post."
                  event.Title.Unwrap
          To = participant.Email
          CalendarInvite = None }

    let private createCancelledEventMail
        (message: string)
        (event: Event)
        noReplyMail
        (participant: Participant)
        =
        { Subject = sprintf "Avlyst: %s" event.Title.Unwrap
          Message = message.Replace("\n", "<br>")
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant, noReplyMail, message, Cancel) |> Some }

    let registerParticipant createMail registration =
        result {

            let! participant = repo.create registration

            yield Service.sendMail (createMail participant)
            return participant
        }

    let getParticipant (eventId, email) =
        result {
            let! participants = repo.read

            let! participant =
                participants
                |> queryParticipantByKey (eventId, email)
                |> ignoreContext

            return participant
        }

    let getParticipantsForEvent (event: Event): Handler<ParticipantsWithWaitingList> =
        result {
            let! participants = repo.read

            let participantsForEvent =
                participants
                |> queryParticipantsBy event.Id
                |> Seq.map models.dbToDomain
                |> Seq.sortBy
                    (fun participant -> participant.RegistrationTime)

            return { attendees =
                         Seq.truncate event.MaxParticipants.Unwrap
                             participantsForEvent

                     waitingList =
                         Seq.safeSkip event.MaxParticipants.Unwrap
                             participantsForEvent }
        }

    let getParticipationsForParticipant email =
        result {
            let! participants = repo.read
            
            let participantsByMail =
                participants |> queryParticipantionByParticipant email

            return Seq.map models.dbToDomain participantsByMail
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
            let mail = createCancelledParticipationMail event participant

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

            repo.del participant

            return participationSuccessfullyDeleted (event.Id, email)
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

        participants |> Seq.iter sendMailToParticipant

        Ok()
