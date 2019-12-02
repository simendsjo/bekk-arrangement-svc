namespace ArrangementService

module Functions =
  type ResultBuilder() =
    member this.Yield(x) = x
    member this.Delay(fn) = fn()
    member this.Combine(a, b) =
      match a, b with
      | Ok _    , Ok _     -> a
      | Ok _    , Error e  -> Error e
      | Error e , Ok _     -> Error e
      | Error e1, Error e2 -> Error (List.concat [e1; e2])
   
  let validator = ResultBuilder()

  let validate f (x: 'a) (errorMessage: string) =
    if f x then
      Ok x
    else
      Error [errorMessage]
