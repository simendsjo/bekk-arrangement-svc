namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService.Repo
open ArrangementService.Database

module Models =

    type DomainModel =
        { Id: int
          Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int }

    type ViewModel =
        { Id: int
          Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int }

    type WriteModel =
        { Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int }

    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``

    type DbModel = ArrangementDbContext.``dbo.EventsEntity``

    type Key = int

    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Id = dbRecord.Id
          Title = dbRecord.Title
          Description = dbRecord.Description
          Location = dbRecord.Location
          FromDate = dbRecord.FromDate
          ToDate = dbRecord.ToDate
          ResponsibleEmployee = dbRecord.ResponsibleEmployee }

    let updateDbWithDomain (db: DbModel) (event: DomainModel) =
        db.Title <- event.Title
        db.Description <- event.Description
        db.Location <- event.Location
        db.FromDate <- event.FromDate
        db.ToDate <- event.ToDate
        db.ResponsibleEmployee <- event.ResponsibleEmployee
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Id = domainModel.Id
          Title = domainModel.Title
          Description = domainModel.Description
          Location = domainModel.Location
          FromDate = domainModel.FromDate
          ToDate = domainModel.ToDate
          ResponsibleEmployee = domainModel.ResponsibleEmployee }

    let writeToDomain (id: Key) (writeModel: WriteModel): DomainModel =
        { Id = id
          Title = writeModel.Title
          Description = writeModel.Description
          Location = writeModel.Location
          FromDate = writeModel.FromDate
          ToDate = writeModel.ToDate
          ResponsibleEmployee = writeModel.ResponsibleEmployee }


    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Key, TableModel> =
        { key = fun record -> record.Id

          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Events

          create = fun table -> table.Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain

          updateDbWithDomain = updateDbWithDomain

          domainToView = domainToView

          writeToDomain = writeToDomain }
