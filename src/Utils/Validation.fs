namespace ArrangementService

open System.Text.RegularExpressions

module Validation =
  type ResultBuilder() =
    member this.Return(x) = Ok x
    member this.ReturnFrom(x) = x
    member this.Bind(x, f) =
      match x with
      | Ok o    -> f o
      | Error e -> Error e
    member this.Yield(x) =
      match x with 
      | Ok o -> Ok o
      | Error e -> Error e
    member this.Delay(f) = f()
    member this.Combine(result1, result2) =
      match result1, result2 with
      | Ok _    , Ok _     -> result1
      | Ok _    , Error e  -> Error e
      | Error e , Ok _     -> Error e
      | Error e1, Error e2 -> Error (List.concat [e1; e2])

  let regexMatch regex input = Regex.IsMatch(input, regex) 

  let validator = ResultBuilder()
  
  let validate f (input: 'Type) errorMessage =
    if f input then
      Ok input
    else
      Error [errorMessage]

  let validateMinLength text length errorMessage =
    validate(fun x -> String.length x > length) text errorMessage
  
  let validateMaxLength text length errorMessage =
    validate(fun x -> String.length x < length) text errorMessage

  let validateEmail email errorMessage = 
      let emailRegex = """^(?(")(".+?(?<!\\)"@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$"""
      validate (fun x -> regexMatch emailRegex x) email errorMessage
 
  let validateBefore before after errorMessage = 
    validate (fun (x, y) -> x < y) (before, after) errorMessage

  let validateAfter before after errorMessage =
    validate (fun (x, y) -> x > y) (before, after) errorMessage
