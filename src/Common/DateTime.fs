namespace ArrangementService

module DateTime =

    open System
    type Date =
        { Day: int
          Month: int
          Year: int }

    type Time =
        { Hour: int
          Minute: int }

    [<CustomComparison; StructuralEquality>]
    type DateTimeCustom =
        { Date: Date
          Time: Time }

        interface IComparable with
          member this.CompareTo obj =
            match obj with
            | :? DateTimeCustom as other -> 
              let thisDateTime = (this.Date.Year, this.Date.Month, this.Date.Day, this.Time.Hour, this.Time.Minute)
              let otherDateTime = (other.Date.Year, other.Date.Month, other.Date.Day, other.Time.Hour, other.Time.Minute)
              if thisDateTime > otherDateTime
              then 1
              else if thisDateTime < otherDateTime
                then -1
                else 0
            | _ -> 0

    let customToDateTime (dateTime : DateTimeCustom) : DateTime =
      let date = dateTime.Date
      let time = dateTime.Time
      DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0)
    
    let customToTimeSpan (time: Time) : TimeSpan =
        TimeSpan(time.Hour, time.Minute, 0)


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
  
    let now (): DateTimeCustom =
      toCustomDateTime DateTime.Now (TimeSpan(0, 0, 0))