namespace ArrangementService

open ArrangementService.DomainModels
open ArrangementService.Participant
open ArrangementService.Email

module BusinessLogic =

    let getAttendeesAndWaitinglist
        (event: Event)
        (participants: Participant seq)
        : Participant.ParticipantsWithWaitingList =
        match event.MaxParticipants.Unwrap with
        // Max participants = 0 means participants = infinity
        | 0 ->
            { attendees = participants
              waitingList = [] }

        | maxParticipants ->
            { attendees = Seq.truncate maxParticipants participants
              waitingList = Seq.safeSkip maxParticipants participants }

    let getWaitinglistSpot (event: Event) (email: EmailAddress) (participants: ParticipantsWithWaitingList) =
        let attendees = participants.attendees 
        let waitingList = participants.waitingList

        let isParticipant =
            Seq.append attendees waitingList
            |> Seq.exists (fun y -> y.Email = email)

        if not isParticipant then
            Error [ Participant.UserMessages.participantNotFound email ]

        else
            waitingList 
            |> Seq.tryFindIndex (fun participant -> participant.Email = email)
            |> Option.map (fun index -> index + 1)
            |> Option.defaultValue 0 
            |> Ok
