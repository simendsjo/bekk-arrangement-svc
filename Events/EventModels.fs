namespace ArrangementService.Events

open System
open ArrangementService.Database

module Models =
    type EventDomainModel =
        { Id: int
          Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int }

    type EventViewModel =
        { Id: int
          Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int }

    type EventWriteModel =
        { Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int }

    // Utils

    let mapDbEventToDomain (dbRecord: ArrangementSql.dataContext.``dbo.EventsEntity``): EventDomainModel =
        { Id = dbRecord.Id
          Title = dbRecord.Title
          Description = dbRecord.Description
          Location = dbRecord.Location
          FromDate = dbRecord.FromDate
          ToDate = dbRecord.ToDate
          ResponsibleEmployee = dbRecord.ResponsibleEmployee }

    let domainToView (domainModel: EventDomainModel): EventViewModel =
        { Id = domainModel.Id
          Title = domainModel.Title
          Description = domainModel.Description
          Location = domainModel.Location
          FromDate = domainModel.FromDate
          ToDate = domainModel.ToDate
          ResponsibleEmployee = domainModel.ResponsibleEmployee }

    let writeToDomain id (writeModel: EventWriteModel): EventDomainModel =
        { Id = id
          Title = writeModel.Title
          Description = writeModel.Description
          Location = writeModel.Location
          FromDate = writeModel.FromDate
          ToDate = writeModel.ToDate
          ResponsibleEmployee = writeModel.ResponsibleEmployee }
