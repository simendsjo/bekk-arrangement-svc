module Tests

open ArrangementService.Utils
open ArrangementService.Event.Models
open ArrangementService.UserMessage

open type ArrangementService.Event.WriteModel

open Expecto
open System
open ArrangementService.BusinessLogic

let compareListOfRecords a b =
  if (Seq.length a) <> (Seq.length b) then
      failwith "Not the same length"
  else 
    Seq.zip a b
    |> Seq.map (fun (x, y) -> Expect.equal x y "These should be equal")
    |> ignore

let genDate year month day : ArrangementService.Date =
    { Day = day
      Month = month
      Year = year }

let genTime hour minute : ArrangementService.Time = { Hour = hour; Minute = minute }

let genDateTime year month day hour minute : ArrangementService.DateTimeCustom =
    { Date = genDate year month day
      Time = genTime hour minute }

let getOrThrowResult result =
    match result with
    | Ok x -> x
    | Error x -> failwith $"{x}"


let genEvent maxParticipants hasWaitingList=
    let writeModel =
        { ArrangementService.Event.WriteModel.Title = "String"
          Description = "String"
          Location = "String"
          OrganizerName = "String"
          OrganizerEmail = "String@"
          MaxParticipants = maxParticipants
          StartDate = genDateTime 1971 1 1 1 1
          EndDate = genDateTime 1971 1 2 1 1
          OpenForRegistrationTime = "1624266180000"
          editUrlTemplate = "hei"
          ParticipantQuestion = Some "hei"
          HasWaitingList = hasWaitingList
          IsExternal = false
          Shortname = None }
    let eventResult =
        writeToDomain (Guid.NewGuid()) writeModel (Guid.NewGuid()) false 1111
    getOrThrowResult eventResult

let genParticipant' email pkModifier = 
  let writeModel = {
    ArrangementService.Participant.WriteModel.name = "Hei"
    ArrangementService.Participant.WriteModel.comment = "Hei"
    ArrangementService.Participant.WriteModel.cancelUrlTemplate = "Hei"
  }
  let participant = ArrangementService.Participant.Models.writeToDomain (Guid.NewGuid(), email+pkModifier) writeModel None
  getOrThrowResult participant

let genParticipant = genParticipant' "TheodorogSara@bekk.no"

let genParticipantsWithGiven (optionalParticpant: ArrangementService.DomainModels.Participant option) numberOfAttendees numberOnWaitinglist =
  let attendees = seq {for i in 0 .. (numberOfAttendees-1) -> genParticipant $"{i}attendees" }
  let waitinglist = seq {for i in 0 .. (numberOnWaitinglist-1) -> genParticipant $"{i}waitinglist"}
  match optionalParticpant with
    | Some participant -> ((Seq.append attendees (seq {participant})),waitinglist)
    | None -> (attendees, waitinglist)

let genParticipants = genParticipantsWithGiven None

// Probably should try some propertybased testing here
let testGetAttendeesAndWaitingList  =
  let maxParticipants = 10
  let hasWaitingList = true
  let numberOnWaitinglist = 3
  let numberOfAttendees = 10
  let event = genEvent maxParticipants hasWaitingList
  let (attendees,waitinglist) = genParticipants numberOfAttendees numberOnWaitinglist
  let result =  {
    ArrangementService.Participant.ParticipantsWithWaitingList.attendees = attendees
    ArrangementService.Participant.ParticipantsWithWaitingList.waitingList = if hasWaitingList then waitinglist else Seq.empty
  }
  let funcCall = getAttendeesAndWaitinglist event (Seq.append attendees waitinglist)
  compareListOfRecords funcCall.attendees result.attendees
  compareListOfRecords funcCall.waitingList result.waitingList


let testWaitingListSpot = 
  let maxParticipants = 10
  let hasWaitingList = true
  let event = genEvent 10 true
  let participant = genParticipant' "test@gmail.com" "" // Generated first
  let (attendees,waitinglist) = genParticipantsWithGiven (Some participant) 10 10 
  let email = participant.Email
  let funcCall = getAttendeesAndWaitinglist event (Seq.append attendees waitinglist)
  let spot = getWaitinglistSpot event email funcCall
  Expect.equal (Ok 0) spot "The participant is attending the event"


[<Tests>]
let tests =
    testList
        "samples"
        [ testCase "universe exists (╭ರᴥ•́)"
          <| fun _ ->
              let subject = true
              Expect.isTrue subject "I compute, therefore I am."


          testCase "I'm skipped (should skip)"
          <| fun _ -> Tests.skiptest "Yup, waiting for a sunny day..."

          testCase "contains things"
          <| fun _ -> Expect.containsAll [| 2; 3; 4 |] [| 2; 4 |] "This is the case; {2,3,4} contains {2,4}"

          testCase "Sometimes I want to ༼ノಠل͟ಠ༽ノ ︵ ┻━┻"
          <| fun _ -> Expect.equal "abcdef" "abcdef" "These should equal"

          testCase "Should return"
          <| fun _ -> Expect.equal (validateNotNegative "" 1) (Ok()) "These should equal"
          testCase "Should be equal"
          <| fun _ -> testGetAttendeesAndWaitingList 

          ]
