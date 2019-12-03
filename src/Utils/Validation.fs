namespace ArrangementService

open System.Text.RegularExpressions

module Validation =
  type ResultBuilder() =
    member this.Yield(x) = x
    member this.Delay(f) = f()
    member this.Combine(result1, result2) =
      match result1, result2 with
      | Ok _    , Ok _     -> result1
      | Ok _    , Error e  -> Error e
      | Error e , Ok _     -> Error e
      | Error e1, Error e2 -> Error (List.concat [e1; e2])

  let RegexMatch regex input =
    if Regex.IsMatch(input, regex) then Some true else None

  let validator = ResultBuilder()

  let validateOne f (input: 'Type) errorMessage =
    if f input then
      Ok input
    else
      Error [errorMessage]

  let validateTwo f (input1: 'Type1) (input2: 'Type2) errorMessage =
    if f input1 input2 then
      Ok (input1, input2)
    else
      Error [errorMessage]

  let validateMinLength text length errorMessage =
    validateOne (fun x -> String.length x > length) text errorMessage
  
  let validateMaxLength text length errorMessage =
    validateOne (fun x -> String.length x < length) text errorMessage

  let validateEmail email errorMessage = 
      let emailRegex = """^(?(")(".+?(?<!\\)"@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$"""
      let validateEmail text =
        match RegexMatch emailRegex text with
        | Some _ -> true
        | None   -> false

      validateOne validateEmail email errorMessage
 
  let validateBefore before after errorMessage = 
    validateTwo (<) before after errorMessage

  let validateAfter before after errorMessage =
    validateTwo (>) before after errorMessage
