namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService.Database
open ArrangementService.Repo
open ArrangementService.Events.ErrorMessages
open ArrangementService.Http
open ArrangementService.Validation

module Models =
    type Id = Id of Guid
    type Title = Title of string
    type Description = Description of string
    type Location = Location of string
    type FromDate = DateTimeOffset
    type ToDate = DateTimeOffset
    type ResponsibleEmployee = ResponsibleEmployee of int
    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``
    type DbModel = ArrangementDbContext.``dbo.EventsEntity``
    type Email = Email of string
    type Date =
        { Day: int
          Month: int
          Year: int }
        
    type Time =
        { Hour: int
          Minute: int }
        
    type DateTimeCustom =
        { Date: Date
          Time: Time }
    
    type DomainModel =
        { Id: Guid
          Title: Title
          Description: Description
          Location: Location
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OrganizerEmail: Email
          OpenForRegistrationDate: DateTimeCustom }

    type ViewModel =
        { Id: Guid
          Title: string
          Description: string
          Location: string
          OrganizerEmail: string
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OpenForRegistrationDate: DateTimeCustom }

    type WriteModel =
        { Title: string
          Description: string
          Location: string
          OrganizerEmail: string
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OpenForRegistrationDate: DateTimeCustom }

    let toCustomDateTime (date: DateTime) (time: TimeSpan): DateTimeCustom =
        {
            Date =
                { Day = date.Day
                  Month = date.Month
                  Year = date.Year }
            Time =
                { Hour = time.Hours
                  Minute = time.Minutes }
        }
    
    let customToDateTime (date : Date) : DateTime =
        DateTime(date.Year, date.Month, date.Day)
    
    let customToTimeSpan (time: Time) : TimeSpan =
        TimeSpan(time.Hour, time.Minute, 0)

    let customDateTimeOffset someTime =
      DateTimeOffset(customToDateTime someTime.Date, customToTimeSpan someTime.Time)

    let unwrapId = function | Id id -> id
    let unwrapTitle = function | Title t -> t
    let unwrapDescription = function | Description d -> d
    let unwrapLocation = function | Location l -> l
    let unwrapEmail = function | Email e -> e

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

    let dateValidator startDate endDate =
      // FromDate: Må være i fremtiden
      // ToDate: Må være i fremtiden og etter fromDate
      let fromDate = customDateTimeOffset startDate
      let toDate = customDateTimeOffset endDate
      let foo fromD toD = 
        if fromD < toD then 
          Ok fromDate 
        else 
          Error ["Til-dato må være etter fra-dato."]
      validator {
        yield validate (fun _ -> fromDate > DateTimeOffset.Now) fromDate "Fra-dato må være i fremtiden"
        yield validate (fun _ -> toDate > DateTimeOffset.Now) toDate "Til-dato må være i fremtiden"
        yield foo fromDate toDate
      }

    let organizerEmailValidator email=
      let emailRegex = "^(?(\"\")(\"\".+?(?<!\\)\"\"@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))"
      let foo text =
        match RegexMatch emailRegex text with
        | Some _ -> true
        | None   -> false
      // Organizer email: Må være en valid epost addresse
      validator {
        yield validate (fun x -> foo x) email "Ansvarlig må ha en gyldig epost-addresse"
      }

    let validateWriteModel (id: Id) (writeModel : WriteModel) : Result<WriteModel, HttpErr> =
      validator {
        yield titleValidator writeModel.Title
        yield descriptionValidator writeModel.Description
        yield locationValidator writeModel.Location
        yield dateValidator writeModel.StartDate writeModel.EndDate
        yield organizerEmailValidator writeModel.OrganizerEmail
      }
      |> function
      | Ok _ -> Ok writeModel
      | Error e -> badRequest id e |> Error
   
    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Id = dbRecord.Id
          Title = Title dbRecord.Title
          Description = Description dbRecord.Description
          Location = Location dbRecord.Location
          OrganizerEmail = Email dbRecord.OrganizerEmail
          StartDate = toCustomDateTime dbRecord.StartDate dbRecord.StartTime
          EndDate = toCustomDateTime dbRecord.EndDate dbRecord.EndTime
          OpenForRegistrationDate = toCustomDateTime dbRecord.OpenForRegistrationDate dbRecord.OpenForRegistrationTime
          }

    let updateDbWithDomain (db: DbModel) (event: DomainModel) =
        db.Title <- unwrapTitle event.Title
        db.Description <- unwrapDescription event.Description
        db.Location <- unwrapLocation event.Location
        db.OrganizerEmail <- unwrapEmail event.OrganizerEmail
        db.StartDate <- customToDateTime event.StartDate.Date
        db.StartTime <- customToTimeSpan event.StartDate.Time
        db.EndDate <- customToDateTime event.EndDate.Date
        db.EndTime <- customToTimeSpan event.EndDate.Time
        db.OpenForRegistrationDate <-customToDateTime event.OpenForRegistrationDate.Date
        db.OpenForRegistrationTime <- customToTimeSpan event.OpenForRegistrationDate.Time
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Id = domainModel.Id
          Title = unwrapTitle domainModel.Title
          Description = unwrapDescription domainModel.Description
          Location = unwrapLocation domainModel.Location
          OrganizerEmail = unwrapEmail domainModel.OrganizerEmail
          StartDate = domainModel.StartDate
          EndDate = domainModel.EndDate
          OpenForRegistrationDate = domainModel.OpenForRegistrationDate }

    let writeToDomain (id: Id) (writeModel: WriteModel): DomainModel =
        { Id = unwrapId id
          Title = Title writeModel.Title
          Description = Description writeModel.Description
          Location = Location writeModel.Location
          OrganizerEmail = Email writeModel.OrganizerEmail
          StartDate = writeModel.StartDate
          EndDate = writeModel.EndDate
          OpenForRegistrationDate = writeModel.OpenForRegistrationDate 
        }

    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Id, TableModel> =
        { key = fun record -> Id record.Id 
          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Events
          create = fun table -> table.Create()
          delete = fun record -> record.Delete()
          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain
          domainToView = domainToView
          writeToDomain = writeToDomain }
