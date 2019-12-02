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

    let idValidator id =
      validator {
        // Id: Må være et positivt tall
        yield validate (fun x -> x > 0) id "Id må være et positivt tall."
      }

    let titleValidator title =
      validator {
        // Title: må være lengre enn 3 og kortere enn 60 bokstaver
        yield validate (fun x -> String.length x > 3) title "Tittel må ha minst 3 tegn" 
        yield validate (fun x -> String.length x < 60) title "Tittel må være mindre enn 60 tegn"
      }

    let descriptionValidator description =
      validator {
        // Description: må være kortere enn 255 bokstaver
        yield validate (fun x -> String.length x > 3) description "Beskrivelse må ha minst 3 tegn"
        yield validate (fun x -> String.length x < 255) description "Beskrivelse må være mindre enn 255 tegn"
      }

    let locationValidator location =
    // Location: må være lengre enn 3 og kortere enn 30 bokstaver
      validator {
        yield validate (fun x -> String.length x > 3) location "Sted må ha minst 3 tegn"
        yield validate (fun x -> String.length x < 30) location "Sted må være mindre enn 30 tegn"
      }

    let dateValidator fromDate toDate =
      // FromDate: Må være i fremtiden
      // ToDate: Må være i fremtiden og etter fromDate
      let something fromD toD = 
        if fromD < toD then 
          Ok fromDate 
        else 
          Error ["Til-dato må være etter fra-dato."]
      validator {
        yield validate (fun _ -> fromDate > DateTimeOffset.Now) fromDate "Fra-dato må være i fremtiden"
        yield validate (fun _ -> toDate > DateTimeOffset.Now) toDate "Til-dato må være i fremtiden"
        yield something fromDate toDate
      }

    let responsibleEmployeeValidator employee =
      // ResponsibleEmployee: Må være et positivt tall
      validator {
        yield validate (fun x -> x > 0) employee "Ansvarlig ansatt id må være et positivt tall"
      }

    let validateWriteModel (id: Id) (writeModel : WriteModel) : Result<WriteModel, HttpErr> =
      validator {
        yield idValidator (unwrapId id)
        yield titleValidator writeModel.Title
        yield descriptionValidator writeModel.Description
        yield locationValidator writeModel.Location
        yield dateValidator writeModel.FromDate writeModel.ToDate
        yield responsibleEmployeeValidator writeModel.ResponsibleEmployee
      }
      |> function
      | Ok _ -> Ok writeModel
      | Error e -> badRequest id e |> Error
   
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
