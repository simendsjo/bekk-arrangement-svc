namespace ArrangementService

open System.Text.RegularExpressions

module Validation =

  type ValidationBuilder() =
    member this.Return(x) = Ok x
    member this.ReturnFrom(x) = x
    member this.Yield(x) = Ok x
    member this.YieldFrom(x) = x

    member this.Bind(rx, f) =
      match rx with
      | Ok x -> f x
      | Error e -> Error e

    member this.Delay(f) = f()

    member this.Combine(rv, rf) =
        match rv, rf with
        | Error vs, Error es -> List.concat [vs; es] |> Error
        | Error vs, Ok _ -> Error vs
        | Ok _, Error es -> Error es
        | Ok v, Ok f -> f v |> Ok

  let validator = ValidationBuilder()

  let regexMatch regex input = Regex.IsMatch(input, regex) 
  
  let validate f (input: 'Type) errorMessage =
    if f input then
      Ok ()
    else
      Error errorMessage

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

  let validateAll constructor thingToValidate validationFunctions =
    validationFunctions
    |> List.map (fun validationFunction -> validationFunction thingToValidate ) 
    |> List.fold
        (fun acc ->
            function
            | Ok () -> acc
            | Error e -> acc @ [e]) []
    |> function
    | [] -> constructor thingToValidate |> Ok
    | errors -> Error errors