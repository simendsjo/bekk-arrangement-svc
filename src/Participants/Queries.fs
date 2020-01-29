namespace ArrangementService.Participant

open UserMessages
open System.Linq

open ArrangementService

open ArrangementService.Email
open ResultComputationExpression

module Queries =

    let queryParticipantByKey (id: Event.Id, email: EmailAddress)
        (participants: DbModel IQueryable) =
        query {
            for participant in participants do
                where
                    (participant.Email = email.Unwrap
                     && participant.EventId = id.Unwrap)
                select (Some participant)
                exactlyOneOrDefault
        }
        |> withError [ participationNotFound (id, email) ]

    let queryParticipantionByParticipant (email: EmailAddress)
        (participants: DbModel IQueryable) =
        query {
            for participant in participants do
                where (participant.Email = email.Unwrap)
                select participant
        }

    let queryParticipantsBy (eventId: Event.Id)
        (participants: DbModel IQueryable) =
        query {
            for participant in participants do
                where (participant.EventId = eventId.Unwrap)
                select participant
        }
