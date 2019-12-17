namespace ArrangementService.Participants

open System
open Giraffe

open ArrangementService
open Http
open Database
open Repo
open ArrangementService.Email.Models

module Models =
    type DomainModel =
        { Email: EmailAddress
          EventId: Guid
          RegistrationTime: int64 }

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
          EventId = dbRecord.EventId 
          RegistrationTime = dbRecord.RegistrationTime }

    let writeToDomain ((id, email): Key) (_: WriteModel): Result<DomainModel, CustomErrorMessage list> =
        Ok { Email = EmailAddress email 
             EventId = id
             RegistrationTime = DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() }

    let updateDbWithDomain (db: DbModel) (domainModel: DomainModel) =
        db.Email <- emailAddressToString domainModel.Email
        db.EventId <- domainModel.EventId
        db.RegistrationTime <- domainModel.RegistrationTime
        db

    let domainToView (domainModel: DomainModel): ViewModel =
      { Email = emailAddressToString domainModel.Email
        EventId = domainModel.EventId
        RegistrationTime = domainModel.RegistrationTime }

    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Key, TableModel> = 
      { key = fun record -> (record.EventId, record.Email)
        table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Participants

        create = fun table -> table.Create()
        delete = fun record -> record.Delete()

        dbToDomain = dbToDomain
        updateDbWithDomain = updateDbWithDomain
        domainToView = domainToView
        writeToDomain = writeToDomain }