namespace ArrangementService.Participants

open System
open Giraffe

open ArrangementService.Database
open ArrangementService.Repo

module Models =
    type DomainModel =
        { ParticipantEmail: string 
          EventId: Guid
          RegistrationTime: int64 }

    type ViewModel =
        { ParticipantEmail: string 
          EventId: Guid
          RegistrationTime: int64 }

    type WriteModel = 
        { ParticipantEmail: string }
    
    type Key = Guid * string 

    type TableModel = ArrangementDbContext.dboSchema.``dbo.Participants``

    type DbModel = ArrangementDbContext.``dbo.ParticipantsEntity``

   
    let dbToDomain (dbRecord: DbModel): DomainModel =
        { ParticipantEmail = dbRecord.Email
          EventId = dbRecord.EventId 
          RegistrationTime = dbRecord.RegistrationTime }

    let writeToDomain ((id, _): Key) (writeModel: WriteModel): DomainModel =
        { ParticipantEmail = writeModel.ParticipantEmail 
          EventId = id
          RegistrationTime = DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() }

    let updateDbWithDomain (db: DbModel) (domainModel: DomainModel) =
        db.Email <- domainModel.ParticipantEmail
        db.EventId <- domainModel.EventId
        db.RegistrationTime <- domainModel.RegistrationTime
        db

    let domainToView (domainModel: DomainModel): ViewModel =
      { ParticipantEmail = domainModel.ParticipantEmail
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