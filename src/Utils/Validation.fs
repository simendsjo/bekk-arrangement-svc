namespace ArrangementService

open System.Text.RegularExpressions

module Validation =
  type ResultBuilder() =
    member this.Yield(x) = x
    member this.Delay(fn) = fn()
    member this.Combine(a, b) =
      match a, b with
      | Ok _    , Ok _     -> a
      | Ok _    , Error e  -> Error e
      | Error e , Ok _     -> Error e
      | Error e1, Error e2 -> Error (List.concat [e1; e2])

  let RegexMatch regex input =
    if Regex.IsMatch(input, regex) then Some true else None

  let validator = ResultBuilder()

  let validate f (x: 'a) (errorMessage: string) =
    if f x then
      Ok x
    else
      Error [errorMessage]
