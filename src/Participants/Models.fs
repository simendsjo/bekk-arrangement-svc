namespace ArrangementService.Participant

open System
open System.Linq
open Giraffe

open ArrangementService

open TimeStamp
open Validation
open Database
open Repo
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels
open Microsoft.AspNetCore.Http

type ViewModel =
    { Email: string
      EventId: string
      RegistrationTime: int64
      CancellationToken: string }

// Empty for now
type WriteModel = Unit

type Key = Guid * string

type DbModel = ArrangementDbContext.``dbo.ParticipantsEntity``

type TableModel = ArrangementDbContext.dboSchema.``dbo.Participants``

module Models =

    let private optionToNullable<'T when 'T :> obj> (maybe: 'T option) =
        if maybe.IsSome then maybe.Value.ToString() else null

    let getParticipants (ctx: HttpContext): TableModel =
        ctx.GetService<ArrangementDbContext>().Dbo.Participants

    let dbToDomain (dbRecord: DbModel): Participant =
        { Email = EmailAddress dbRecord.Email
          EventId = Event.Id dbRecord.EventId
          RegistrationTime = TimeStamp dbRecord.RegistrationTime
          CancellationToken = dbRecord.CancellationToken |> Some }

    let writeToDomain ((id, email): Key) ((): WriteModel): Result<Participant, UserMessage list> =
        Ok Participant.Create <*> EmailAddress.Parse email
        <*> (Event.Id id |> Ok) <*> (now() |> Ok)

    let updateDbWithDomain (db: DbModel) (participant: Participant) =
        db.Email <- participant.Email.Unwrap
        db.EventId <- participant.EventId.Unwrap
        db.RegistrationTime <- participant.RegistrationTime.Unwrap
        db

    let domainToView (participant: Participant): ViewModel =
        { Email = participant.Email.Unwrap
          EventId = participant.EventId.Unwrap.ToString()
          RegistrationTime = participant.RegistrationTime.Unwrap
          CancellationToken = optionToNullable participant.CancellationToken }

    let models: Models<DbModel, Participant, ViewModel, WriteModel, Key, IQueryable<DbModel>> =
        { key = fun record -> (record.EventId, record.Email)

          table = fun ctx -> getParticipants ctx :> IQueryable<DbModel>
          create = fun ctx -> (getParticipants ctx).Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain
          domainToView = domainToView
          writeToDomain = writeToDomain }
