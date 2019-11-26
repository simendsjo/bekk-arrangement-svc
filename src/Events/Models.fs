namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService.Database
open ArrangementService.Repo

module Models =

    type DomainModel =
        { Id: Guid
          Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          OrganizerEmail: string }

    type ViewModel =
        { Id: Guid
          Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          OrganizerEmail: string }

    type WriteModel =
        { Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          OrganizerEmail: string }

    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``

    type DbModel = ArrangementDbContext.``dbo.EventsEntity``

    type Key = Guid

    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Id = dbRecord.Id
          Title = dbRecord.Title
          Description = dbRecord.Description
          Location = dbRecord.Location
          FromDate = dbRecord.FromDate
          ToDate = dbRecord.ToDate
          OrganizerEmail = dbRecord.OrganizerEmail }

    let updateDbWithDomain (db: DbModel) (event: DomainModel) =
        db.Title <- event.Title
        db.Description <- event.Description
        db.Location <- event.Location
        db.FromDate <- event.FromDate
        db.ToDate <- event.ToDate
        db.OrganizerEmail <- event.OrganizerEmail
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Id = domainModel.Id
          Title = domainModel.Title
          Description = domainModel.Description
          Location = domainModel.Location
          FromDate = domainModel.FromDate
          ToDate = domainModel.ToDate
          OrganizerEmail = domainModel.OrganizerEmail }

    let writeToDomain (id: Key) (writeModel: WriteModel): DomainModel =
        { Id = id
          Title = writeModel.Title
          Description = writeModel.Description
          Location = writeModel.Location
          FromDate = writeModel.FromDate
          ToDate = writeModel.ToDate
          OrganizerEmail = writeModel.OrganizerEmail }

    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Key, TableModel> =
        { key = fun record -> record.Id

          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Events

          create = fun table -> table.Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain

          updateDbWithDomain = updateDbWithDomain

          domainToView = domainToView

          writeToDomain = writeToDomain }
