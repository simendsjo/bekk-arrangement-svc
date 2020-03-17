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
open ArrangementService.Config
open Http

module Service =

    let repo = Repo.from models

    let private inviteMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Meld deg av her: %s." redirectUrl
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

    let private waitlistedMessage redirectUrl (event: Event) =
        [ "Hei! 😄"
          sprintf "Du er nå på venteliste for %s på %s den %s."
              event.Title.Unwrap event.Location.Unwrap
              (toReadableString event.StartDate)
          "Du vil få beskjed på e-post om du rykker opp fra ventelisten."
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Meld deg av her: %s." redirectUrl
          "NB! Ta vare på lenken til senere - om du rykker opp fra ventelisten bruker du fortsatt denne til å melde deg av."
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "<br>"

    let createNewParticipantMail
        createCancelUrl
        (event: Event)
        isWaitlisted
        fromMail
        (participant: Participant)
        =
        let message =
            if isWaitlisted
            then waitlistedMessage (createCancelUrl participant) event
            else inviteMessage (createCancelUrl participant) event
        { Subject = event.Title.Unwrap
          Message = message
          From = fromMail
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant.Email, message, Create) |> Some }

    let private createCancelledParticipationMail
        (event: Event)
        (participant: Participant)
        fromMail
        =
        { Subject = "Avmelding"
          Message =
              sprintf "%s har meldt seg av %s" participant.Name.Unwrap
                  event.Title.Unwrap
          From = fromMail
          To = event.OrganizerEmail
          CalendarInvite = None }

    let private createFreeSpotAvailableMail
        (event: Event)
        (participant: Participant)
        fromMail
        =
        { Subject = sprintf "Du har fått plass på %s!" event.Title.Unwrap
          Message =
              sprintf
                  "Du har rykket opp fra ventelisten for %s! Hvis du ikke lenger kan delta, meld deg av med lenken fra forrige e-post."
                  event.Title.Unwrap
          From = fromMail
          To = participant.Email
          CalendarInvite = None }

    let private createCancelledEventMail
        (message: string)
        (event: Event)
        (participant: Participant)
        =
        { Subject = sprintf "Avlyst: %s" event.Title.Unwrap
          Message = message.Replace("\n", "<br>")
          From = event.OrganizerEmail
          To = participant.Email
          CalendarInvite =
              createCalendarAttachment
                  (event, participant.Email, message, Cancel) |> Some }

    let registerParticipant createMail registration =
        result {

            for participant in repo.create registration do

                yield Service.sendMail (createMail participant)
                return participant
        }

    let getParticipant (eventId, email) =
        result {
            for participants in repo.read do

                let! participant = participants
                                   |> queryParticipantByKey (eventId, email)

                return participant
        }

    let getParticipantsForEvent (event: Event): Handler<ParticipantsWithWaitingList> =
        result {
            for participants in repo.read do

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
            for participants in repo.read do
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
            for config in getConfig >> Ok do

                match personWhoGotIt with
                | None -> return ()
                | Some participant ->
                    yield Service.sendMail
                              (createFreeSpotAvailableMail event participant
                                   (EmailAddress config.noReplyEmail))
        }

    let private sendMailToOrganizerAboutCancellation event participant =
        result {
            for config in getConfig >> Ok do
                let mail =
                    createCancelledParticipationMail event participant
                        (EmailAddress config.noReplyEmail)
                yield Service.sendMail mail
        }

    let private sendParticipantCancelMails event email =
        result {
            for participants in getParticipantsForEvent event do

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
            for participant in getParticipant (event.Id, email) do

                repo.del participant

                return participationSuccessfullyDeleted (event.Id, email)
        }


    let sendCancellationMailToParticipants
        messageToParticipants
        participants
        event
        ctx
        =
        let sendMailToParticipant participant =
            Service.sendMail
                (createCancelledEventMail messageToParticipants event
                     participant) ctx

        participants |> Seq.iter sendMailToParticipant

        Ok()
