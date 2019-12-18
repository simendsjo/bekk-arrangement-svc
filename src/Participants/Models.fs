namespace ArrangementService.Participants

open System
open Giraffe

open ArrangementService

open TimeStamp
open Validation
open Database
open Repo
open Email.Models
open DomainModel
open CustomErrorMessage

module Models =

    type ViewModel =
        { Email: string
          EventId: Guid
          RegistrationTime: int64 }

    // Empty for now
    type WriteModel = Unit

    type Key = Guid * string

    type TableModel = ArrangementDbContext.dboSchema.``dbo.Participants``
    type DbModel = ArrangementDbContext.``dbo.ParticipantsEntity``


    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Email = EmailAddress dbRecord.Email
          EventId = Events.DomainModel.Id dbRecord.EventId
          RegistrationTime = TimeStamp dbRecord.RegistrationTime }

    let writeToDomain ((id, email): Key) ((): WriteModel): Result<DomainModel, CustomErrorMessage list> =
        Ok DomainModel.Create
          <*> EmailAddress.Parse email
          <*> (Events.DomainModel.Id id |> Ok)
          <*> (now () |> Ok)

    let updateDbWithDomain (db: DbModel) (domainModel: DomainModel) =
        db.Email <- domainModel.Email.Unwrap
        db.EventId <- domainModel.EventId.Unwrap
        db.RegistrationTime <- domainModel.RegistrationTime.Unwrap
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Email = domainModel.Email.Unwrap
          EventId = domainModel.EventId.Unwrap
          RegistrationTime = domainModel.RegistrationTime.Unwrap }

    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Key, TableModel> =
        { key = fun record -> (record.EventId, record.Email)
          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Participants

          create = fun table -> table.Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain
          domainToView = domainToView
          writeToDomain = writeToDomain }
