namespace ArrangementService.Events

open Models
open DomainModel
open System.Linq

module Queries =

    let queryEventBy (id: Id) (events: IQueryable<DbModel>) =
        query {
            for event in events do
                where (event.Id = id.Unwrap)
                select (Some event)
                exactlyOneOrDefault
        }

    let queryEventsOrganizedBy (organizerEmail: string) (events: IQueryable<DbModel>) =
        query {
            for event in events do
                where (event.OrganizerEmail = organizerEmail)
                select event
        }
