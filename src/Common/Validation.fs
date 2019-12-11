namespace ArrangementService

open Utils.Validation

module Validation =

  let validateMinLength length errorMessage text =
    validate (fun x -> String.length x > length) text errorMessage
  
  let validateMaxLength length errorMessage text =
    validate (fun x -> String.length x < length) text errorMessage

  let validateEmail errorMessage email  = 
      let emailRegex = """^(?(")(".+?(?<!\\)"@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$"""
      validate (fun x -> regexMatch emailRegex x) email errorMessage
 
  let validateBefore errorMessage (before, after) = 
    validate (fun (x, y) -> x < y) (before, after) errorMessage

  let validateAfter errorMessage (before, after) =
    validate (fun (x, y) -> x > y) (before, after) errorMessage