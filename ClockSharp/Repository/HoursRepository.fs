namespace ClockSharp.HoursRepository

open ClockSharp.Model

type IHoursRepository = 
   abstract GetTimeRecords : unit -> TimeRecords
   abstract Update : TimeRecord -> IHoursRepository option
   abstract Insert : times:TimeRecords -> IHoursRepository option

