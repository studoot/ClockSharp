module ClockSharp.HoursRepository.Interface

open ClockSharp.Model

type IHoursRepository = 
   abstract GetTimeRecords : unit -> TimeRecords
   abstract Update : TimeRecord -> IHoursRepository option
   abstract Insert : times:TimeRecords -> IHoursRepository option

type NullHoursRepository() =
   interface IHoursRepository with
      member __.GetTimeRecords () = Seq.empty
      member this.Update _ = this :> IHoursRepository |> Some
      member this.Insert _ = this :> IHoursRepository |> Some