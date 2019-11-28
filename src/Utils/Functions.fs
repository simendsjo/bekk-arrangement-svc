namespace ArrangementService

module Functions =
  type ValidationBuilder() =
    member this.Return(x) = x
    member this.Combine(a, b) = a && b
    member this.Delay(fn) = fn()

  let validator = ValidationBuilder()

  let stringLength f length (s : string) = f (String.length s) length
