namespace ArrangementService.Participant

open System

open ArrangementService
open TimeStamp
open Validation
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels

type DbModel =
  { Name: string
    Email: string
    Comment: string
    RegistrationTime: int64
    EventId: Guid
    CancellationToken: Guid
    EmployeeId: int option
  }

type ViewModel =
    { Name: string
      Email: string option
      Comment: string option
      EventId: string
      RegistrationTime: int64
      EmployeeId: int option
    }

type NewlyCreatedParticipationViewModel =
    { Participant: ViewModel
      CancellationToken: string
    }

type WriteModel =
    { name: string
      comment: string
      cancelUrlTemplate: string 
    }

type ParticipantsWithWaitingList =
    { attendees: Participant seq
      waitingList: Participant seq
    }

type ParticipantViewModelsWithWaitingList =
    { attendees: ViewModel list
      waitingList: ViewModel list option
    }

type Key = Guid * string

type EditableEventLocalStorage = {
  EventId: Guid
  EditToken: Guid
}
type ParticipationsLocalStorage = {
  EventId: Guid
  Email: string
  CancellationToken: Guid
}
type ViewModelLocalStorage =
  { EditableEvents: EditableEventLocalStorage list
    Participations: ParticipationsLocalStorage list
  }

module Models =

    let dbToDomain (dbRecord: DbModel): Participant =
        { Name = Name dbRecord.Name
          Email = EmailAddress dbRecord.Email
          Comment = Comment dbRecord.Comment
          EventId = Event.Id dbRecord.EventId
          RegistrationTime = TimeStamp dbRecord.RegistrationTime
          CancellationToken = dbRecord.CancellationToken
          EmployeeId = Participant.EmployeeId dbRecord.EmployeeId
        }

    let writeToDomain ((eventId, email): Key) (writeModel: WriteModel) (employeeId: int option): Result<Participant, UserMessage list> =
          Ok Participant.Create 
          <*> Name.Parse writeModel.name
          <*> EmailAddress.Parse email 
          <*> Comment.Parse writeModel.comment
          <*> (Event.Id eventId |> Ok) 
          <*> (now() |> Ok) 
          <*> (Guid.NewGuid() |> Ok)
          <*> Ok (Participant.EmployeeId employeeId)
    

    let domainToView (participant: Participant): ViewModel =
        { Name = participant.Name.Unwrap
          Email = Some participant.Email.Unwrap 
          Comment = Some participant.Comment.Unwrap
          EventId = participant.EventId.Unwrap.ToString()
          RegistrationTime = participant.RegistrationTime.Unwrap
          EmployeeId = participant.EmployeeId.Unwrap 
        }


    let domainToDb (participant: Participant): DbModel =
        { Name = participant.Name.Unwrap
          Email = participant.Email.Unwrap
          Comment = participant.Comment.Unwrap
          EventId = participant.EventId.Unwrap
          RegistrationTime = participant.RegistrationTime.Unwrap
          CancellationToken = participant.CancellationToken
          EmployeeId = participant.EmployeeId.Unwrap
        }

    let domainToViewWithCancelInfo (participant: Participant): NewlyCreatedParticipationViewModel
        =
        { Participant = domainToView participant
          CancellationToken = participant.CancellationToken.ToString() }

    let domainToLocalStorageView events (participations: Participant seq) : ViewModelLocalStorage = 
        let eventToLocalStorage event = { EventId=event.Id.Unwrap
                                          EditToken = event.EditToken
                                        }
        let participationToLocalStorage (participant:Participant) = { EventId=participant.EventId.Unwrap
                                                                      Email= participant.Email.Unwrap
                                                                      CancellationToken=participant.CancellationToken
                                                                    }

        { EditableEvents = (events |> Seq.map eventToLocalStorage |> Seq.toList)
          Participations = participations |> Seq.map participationToLocalStorage |> Seq.toList
        }