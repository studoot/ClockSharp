namespace ClockSharp.ViewModel

open System
open System.ComponentModel
open ClockSharp.HoursRepository.Interface
open ClockSharp.Model

type HoursRecord = 
   { Date : DateTime
     Start : TimeSpan
     Finish : TimeSpan
     Hours : TimeSpan
     Overtime : TimeSpan }

type Hours() = 
   let timeRecordToHoursRecord (timeRecord:TimeRecord) : HoursRecord = 
      let minutesWorked = (TimeDiff timeRecord.Start timeRecord.Finish) / 1<min>
      let timeWorked = TimeSpan(0, minutesWorked, 0) - TimeSpan(0, 30, 0)
      { Date = DateToDateTime timeRecord.Date
        Start = TimePointToTimeSpan timeRecord.Start
        Finish = TimePointToTimeSpan timeRecord.Finish
        Hours = timeWorked
        Overtime = timeWorked - TimeSpan(7, 36, 0) }

   let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()

   let mutable repo = new NullHoursRepository() :> IHoursRepository

   interface INotifyPropertyChanged with
      [<CLIEvent>]
      member x.PropertyChanged = propertyChangedEvent.Publish

   member x.OnPropertyChanged propertyName = 
      propertyChangedEvent.Trigger([| x; new PropertyChangedEventArgs(propertyName) |])

   member x.Repository
      with get() = repo
      and set(newRepo) = repo <- newRepo ; x.OnPropertyChanged "Hours"

   member val Today = DateTime.Now with get, set

   member x.Hours = 
      x.Repository.GetTimeRecords() |> Seq.map timeRecordToHoursRecord

   member __.CurrentTime = 
      let now = DateTime.Now
      now.ToLongDateString() + " " + now.ToLongTimeString() 

   member x.UpdateFor(instant: DateTime) =
      match TimeRecordForDate instant (x.Repository.GetTimeRecords()) with
      | Some existing -> { existing with Finish=DateTimeToTimePoint instant }
      | _ -> DateTimeToTimeRecord instant

