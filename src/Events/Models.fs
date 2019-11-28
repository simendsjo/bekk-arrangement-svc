namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService.Database
open ArrangementService.Repo
open ArrangementService.Events.ErrorMessages
open ArrangementService.Http
open ArrangementService.Functions

module Models =
  
    type Id = Id of int// Guid
    type Title = Title of string
    type Description = Description of string
    type Location = Location of string
    type FromDate = DateTimeOffset
    type ToDate = DateTimeOffset
    type ResponsibleEmployee = ResponsibleEmployee of int
    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``
    type DbModel = ArrangementDbContext.``dbo.EventsEntity``

     type DomainModel =
        { Id: Id
          Title: Title
          Description: Description
          Location: Location
          FromDate: FromDate
          ToDate: ToDate
          ResponsibleEmployee: ResponsibleEmployee }

    type ViewModel =
        { Id: int
          Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int}

    type WriteModel =
        { Title: string
          Description: string
          Location: string
          FromDate: DateTimeOffset
          ToDate: DateTimeOffset
          ResponsibleEmployee: int }

    let unwrapId = function | Id id -> id
    let unwrapTitle = function | Title t -> t
    let unwrapDescription = function | Description d -> d
    let unwrapLocation = function | Location l -> l
    let unwrapEmployee = function | ResponsibleEmployee re -> re

    let validateId id = unwrapId id > 0
    let validateTitle title = 
      let title = unwrapTitle title
      let isShorterThan title = stringLength (<) 60 title
      let isLongerThan title = stringLength (>) 3 title

      validator {
          return isShorterThan title
          return isLongerThan title
        }

    // Id: Må være et positivt tall
    // Title: må være lengre enn 3 og kortere enn 60 bokstaver
    // Description: må være kortere enn 255 bokstaver
    // Location: må være lengre enn 3 og kortere enn 30 bokstaver
    // FromDate: Må være i fremtiden
    // ToDate: Må være i fremtiden og etter fromDate
    // ResponsibleEmployee: Må være et positivt tall

    let validateWriteModel (id: Id) (writeModel : WriteModel) : Result<WriteModel, HttpErr> =
      validator {
        return validateId id
        return validateTitle (Title writeModel.Title)
      }
      |> function
      | true -> Ok writeModel
      | false -> invalidWriteModel id |> Error
   
    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Id = dbRecord.Id |> Id
          Title = dbRecord.Title |> Title
          Description = dbRecord.Description |> Description
          Location = dbRecord.Location |> Location
          FromDate = dbRecord.FromDate 
          ToDate = dbRecord.ToDate 
          ResponsibleEmployee = dbRecord.ResponsibleEmployee |> ResponsibleEmployee}

    let updateDbWithDomain (db: DbModel) (event: DomainModel) =
        db.Title <- unwrapTitle event.Title
        db.Description <- unwrapDescription event.Description
        db.Location <- unwrapLocation event.Location
        db.FromDate <- event.FromDate
        db.ToDate <- event.ToDate
        db.ResponsibleEmployee <- unwrapEmployee event.ResponsibleEmployee
        db

    let domainToView (domainModel: DomainModel): ViewModel =
      { Id = unwrapId domainModel.Id
        Title = unwrapTitle domainModel.Title 
        Description = unwrapDescription domainModel.Description
        Location = unwrapLocation domainModel.Location
        FromDate = domainModel.FromDate
        ToDate = domainModel.ToDate
        ResponsibleEmployee = unwrapEmployee domainModel.ResponsibleEmployee }

    let writeToDomain id (writeModel: WriteModel): DomainModel =
        { Id = id
          Title = Title writeModel.Title
          Description = Description writeModel.Description
          Location = Location writeModel.Location
          FromDate = writeModel.FromDate
          ToDate = writeModel.ToDate
          ResponsibleEmployee = ResponsibleEmployee writeModel.ResponsibleEmployee }

    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Id, TableModel> =
        { key = fun record -> Id record.Id 

          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Events

          create = fun table -> table.Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain

          updateDbWithDomain = updateDbWithDomain

          domainToView = domainToView

          writeToDomain = writeToDomain }
