namespace ArrangementService.Event

open System

open ArrangementService

open ArrangementService.Event
open Validation
open DateTime
open Utils
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels

type ParticipantQuestionDbModel = {
  Id: int
  EventId: Guid
  Question: string
}

type ShortnameDbModel = {
  Shortname: string 
  EventId: Guid
}

type ViewModel =
    { Id: Guid
      Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int option
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      ParticipantQuestions: string list
      OpenForRegistrationTime: int64 
      CloseRegistrationTime: int64 option
      HasWaitingList: bool 
      IsCancelled: bool 
      IsExternal: bool
      IsHidden: bool
      OrganizerId: int
      Shortname: string option
      CustomHexColor: string option
    }

type ViewModelWithEditToken =
    { Event: ViewModel
      EditToken: string
    }

type WriteModel =
    { Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int option
      StartDate: DateTimeCustom
      EndDate: DateTimeCustom
      OpenForRegistrationTime: string
      CloseRegistrationTime: string option
      ParticipantQuestions: string list
      viewUrl: string option
      editUrlTemplate: string
      HasWaitingList: bool 
      IsExternal: bool
      IsHidden: bool
      Shortname: string option
      CustomHexColor: string option
    }
    
[<CLIMutable>]
type DbModel = 
    { Id: Guid
      Title: string
      Description: string
      Location: string
      OrganizerName: string
      OrganizerEmail: string
      MaxParticipants: int option
      StartDate: DateTime
      EndDate: DateTime
      StartTime: TimeSpan
      EndTime: TimeSpan
      OpenForRegistrationTime: int64
      CloseRegistrationTime: int64 option
      HasWaitingList: bool 
      IsCancelled: bool
      IsExternal: bool 
      IsHidden: bool
      EditToken: Guid
      OrganizerId: int
      CustomHexColor: string option
    }
    static member CreateFromWrite =
        fun (employeeId: int, writeModel: WriteModel) ->
            { Id = Guid.NewGuid()
              Title = writeModel.Title
              Description = writeModel.Description
              Location = writeModel.Location
              OrganizerName = writeModel.OrganizerName
              OrganizerEmail = writeModel.OrganizerEmail
              MaxParticipants = writeModel.MaxParticipants
              StartDate = customToDateTime writeModel.StartDate.Date
              EndDate = customToDateTime writeModel.EndDate.Date
              StartTime = customToTimeSpan writeModel.StartDate.Time
              EndTime = customToTimeSpan writeModel.EndDate.Time
              OpenForRegistrationTime = int64 writeModel.OpenForRegistrationTime
              CloseRegistrationTime = Option.map int64 writeModel.CloseRegistrationTime
              HasWaitingList = writeModel.HasWaitingList
              IsCancelled = false
              IsExternal = writeModel.IsExternal
              IsHidden = writeModel.IsHidden
              EditToken = Guid.NewGuid()
              OrganizerId = employeeId
              CustomHexColor = writeModel.CustomHexColor }

module Models =

    let writeToDomain
        (id: Key)
        (writeModel: WriteModel)
        (editToken: Guid)
        (isCancelled: bool)
        (organizerId: int)
        : Result<Event, UserMessage list>
        =
        Ok Event.Create 
        <*> (Id id |> Ok) 
        <*> Title.Parse writeModel.Title
        <*> Description.Parse writeModel.Description
        <*> Location.Parse writeModel.Location
        <*> OrganizerName.Parse writeModel.OrganizerName
        <*> EmailAddress.Parse writeModel.OrganizerEmail
        <*> MaxParticipants.Parse writeModel.MaxParticipants
        <*> validateDateRange writeModel.StartDate writeModel.EndDate
        <*> OpenForRegistrationTime.Parse writeModel.OpenForRegistrationTime
        <*> CloseRegistrationTime.Parse writeModel.CloseRegistrationTime
        <*> Ok editToken
        <*> ParticipantQuestions.Parse writeModel.ParticipantQuestions
        <*> (writeModel.HasWaitingList |> Ok)
        <*> Ok isCancelled
        <*> (writeModel.IsExternal |> Ok)
        <*> (writeModel.IsHidden |> Ok)
        <*> (EmployeeId organizerId |> Ok)
        <*> Shortname.Parse writeModel.Shortname
        <*> CustomHexColor.Parse writeModel.CustomHexColor

    let dbToDomain (dbRecord: DbModel, participantQuestions: string list, shortname: string option): Event =
        { Id = Id dbRecord.Id
          Title = Title dbRecord.Title
          Description = Description dbRecord.Description
          Location = Location dbRecord.Location
          OrganizerName = OrganizerName dbRecord.OrganizerName
          OrganizerEmail = EmailAddress dbRecord.OrganizerEmail
          MaxParticipants = MaxParticipants dbRecord.MaxParticipants
          StartDate = toCustomDateTime dbRecord.StartDate dbRecord.StartTime
          EndDate = toCustomDateTime dbRecord.EndDate dbRecord.EndTime
          OpenForRegistrationTime =
              OpenForRegistrationTime dbRecord.OpenForRegistrationTime
          CloseRegistrationTime =
              CloseRegistrationTime dbRecord.CloseRegistrationTime
          EditToken = dbRecord.EditToken
          HasWaitingList = dbRecord.HasWaitingList
          IsCancelled = dbRecord.IsCancelled
          IsExternal = dbRecord.IsExternal
          IsHidden = dbRecord.IsHidden
          ParticipantQuestions = ParticipantQuestions participantQuestions
          OrganizerId = EmployeeId dbRecord.OrganizerId
          Shortname = Shortname shortname
          CustomHexColor = CustomHexColor dbRecord.CustomHexColor
        }
        
    let domainToDb (domainModel: Event): DbModel =
        { Id = domainModel.Id.Unwrap
          Title = domainModel.Title.Unwrap
          Description = domainModel.Description.Unwrap
          Location = domainModel.Location.Unwrap
          OrganizerName = domainModel.OrganizerName.Unwrap
          OrganizerEmail = domainModel.OrganizerEmail.Unwrap
          MaxParticipants = domainModel.MaxParticipants.Unwrap
          StartDate = customToDateTime domainModel.StartDate.Date
          StartTime = customToTimeSpan domainModel.StartDate.Time
          EndDate = customToDateTime domainModel.EndDate.Date
          EndTime = customToTimeSpan domainModel.EndDate.Time
          OpenForRegistrationTime = domainModel.OpenForRegistrationTime.Unwrap
          CloseRegistrationTime = domainModel.CloseRegistrationTime.Unwrap
          EditToken = domainModel.EditToken
          HasWaitingList = domainModel.HasWaitingList
          IsCancelled = domainModel.IsCancelled
          IsExternal = domainModel.IsExternal
          IsHidden = domainModel.IsHidden
          OrganizerId = domainModel.OrganizerId.Unwrap
          CustomHexColor = domainModel.CustomHexColor.Unwrap
        }
        

    let domainToView (domainModel: Event): ViewModel =
        { Id = domainModel.Id.Unwrap
          Title = domainModel.Title.Unwrap
          Description = domainModel.Description.Unwrap
          Location = domainModel.Location.Unwrap
          OrganizerName = domainModel.OrganizerName.Unwrap
          OrganizerEmail = domainModel.OrganizerEmail.Unwrap
          MaxParticipants = domainModel.MaxParticipants.Unwrap
          StartDate = domainModel.StartDate
          EndDate = domainModel.EndDate
          OpenForRegistrationTime = domainModel.OpenForRegistrationTime.Unwrap
          CloseRegistrationTime = domainModel.CloseRegistrationTime.Unwrap
          HasWaitingList = domainModel.HasWaitingList 
          IsCancelled = domainModel.IsCancelled 
          IsExternal = domainModel.IsExternal
          IsHidden = domainModel.IsHidden
          ParticipantQuestions = domainModel.ParticipantQuestions.Unwrap
          OrganizerId = domainModel.OrganizerId.Unwrap
          Shortname = domainModel.Shortname.Unwrap
          CustomHexColor = domainModel.CustomHexColor.Unwrap
        }

    let domainToViewWithEditInfo (event: Event): ViewModelWithEditToken =
        { Event = domainToView event
          EditToken = event.EditToken.ToString()
        }
