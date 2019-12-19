namespace ArrangementService.Participants

open Models
open System.Linq

open ArrangementService
open Email.Models

module Queries =

    let queryParticipantByKey
        (id: Events.DomainModel.Id, email: EmailAddress)
        (participants: DbModel IQueryable) =
            query {
                for participant in participants do
                    where (participant.Email = email.Unwrap && participant.EventId = id.Unwrap)
                    select (Some participant)
                    exactlyOneOrDefault
            }

    let queryParticipantBy (email: EmailAddress) (participants: DbModel IQueryable) =
        query {
            for participant in participants do
                where (participant.Email = email.Unwrap)
                select participant
        }
