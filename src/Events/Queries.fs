namespace ArrangementService.Events

open Models
open UserMessages
open DomainModel
open System.Linq

open ArrangementService
open ResultComputationExpression

module Queries =

    let queryEventBy (id: Id) (events: IQueryable<DbModel>) =
        query {
            for event in events do
                where (event.Id = id.Unwrap)
                select (Some event)
                exactlyOneOrDefault
        } |> withError [ eventNotFound id ]

    let queryEventsOrganizedBy (organizerEmail: string) (events: IQueryable<DbModel>) =
        query {
            for event in events do
                where (event.OrganizerEmail = organizerEmail)
                select event
        }
