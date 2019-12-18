namespace ArrangementService

module DateTime =

    open System

    [<CustomComparison; StructuralEquality>]
    type Date =
        { Day: int
          Month: int
          Year: int }

        interface IComparable with
            member this.CompareTo obj =
                match obj with
                | :? Date as other ->
                    let thisDate = (this.Year, this.Month, this.Day)
                    let otherDate = (other.Year, other.Month, other.Day)
                    if thisDate > otherDate then 1
                    else if thisDate < otherDate then -1
                    else 0
                | _ -> 0

    [<CustomComparison; StructuralEquality>]
    type Time =
        { Hour: int
          Minute: int }

        interface IComparable with
            member this.CompareTo obj =
                match obj with
                | :? Time as other ->
                    let thisTime = (this.Hour, this.Minute)
                    let otherTime = (other.Hour, other.Minute)
                    if thisTime > otherTime then 1
                    else if thisTime < otherTime then -1
                    else 0
                | _ -> 0


    [<CustomComparison; StructuralEquality>]
    type DateTimeCustom =
        { Date: Date
          Time: Time }

        interface IComparable with
            member this.CompareTo obj =
                match obj with
                | :? DateTimeCustom as other ->
                    if this.Date > other.Date then 1
                    else if this.Date < other.Date then -1
                    else if this.Time > other.Time then 1
                    else if this.Time < other.Time then -1
                    else 0
                | _ -> 0

    let customToDateTime (date: Date): DateTime = DateTime(date.Year, date.Month, date.Day, 0, 0, 0)

    let customToTimeSpan (time: Time): TimeSpan = TimeSpan(time.Hour, time.Minute, 0)


    let toCustomDateTime (date: DateTime) (time: TimeSpan): DateTimeCustom =
        { Date =
              { Day = date.Day
                Month = date.Month
                Year = date.Year }
          Time =
              { Hour = time.Hours
                Minute = time.Minutes } }

    let now(): DateTimeCustom = toCustomDateTime DateTime.Now (TimeSpan(0, 0, 0))
