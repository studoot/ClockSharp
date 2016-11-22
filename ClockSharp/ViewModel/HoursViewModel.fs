namespace ClockSharp.ViewModel

open ClockSharp.HoursRepository.Interface
open ClockSharp.Model
open System
open System.ComponentModel
open System.Windows.Threading

type HoursRecord = 
   { Date : DateTime
     Start : TimeSpan
     Finish : TimeSpan
     Hours : TimeSpan
     Overtime : TimeSpan }

type ViewModelBase() = 
   let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
   
   interface INotifyPropertyChanged with
      [<CLIEvent>]
      member x.PropertyChanged = propertyChangedEvent.Publish
   
   member x.OnPropertyChanged propertyName = 
      propertyChangedEvent.Trigger([| x
                                      new PropertyChangedEventArgs(propertyName) |])

type Hours() as self = 
   inherit ViewModelBase()
   let mutable repo = new NullHoursRepository() :> IHoursRepository
   
   let timeRecordToHoursRecord (timeRecord : TimeRecord) : HoursRecord = 
      let minutesWorked = MinutesWorked timeRecord
      let overtimeMinutesWorked = max 0 (minutesWorked - StandardDay)
      { Date = DateToDateTime timeRecord.Date
        Start = TimePointToTimeSpan timeRecord.Start
        Finish = TimePointToTimeSpan timeRecord.Finish
        Hours = TimeSpan(0, minutesWorked, 0)
        Overtime = TimeSpan(0, overtimeMinutesWorked, 0) }
   
   let updateTimer = new DispatcherTimer(DispatcherPriority.Input)
   
   let updateRepository instant = 
      match repo.Update (DateTimeToDate instant) (DateTimeToTimePoint instant) with
      | Some newRepo -> 
         repo <- newRepo
         true
      | _ -> false
   
   let hoursUpdater = 
      updateTimer.Tick
      |> Observable.map (fun _ -> DateTime.Now)
      |> Observable.filter (fun instant -> instant.Second = 0)
      |> Observable.subscribe self.UpdateFor
   
   do 
      updateTimer.Interval <- TimeSpan.FromMilliseconds(500.0)
      updateTimer.IsEnabled <- true
   
   member x.Repository 
      with get () = repo
      and set (newRepo) = 
         repo <- newRepo
         ignore <| updateRepository DateTime.Now
         x.OnPropertyChanged "Hours"
   
   member x.Hours = 
      let startDate = DateTime.Now.AddDays(-7.0) |> DateTimeToDate
      
      let foundRecords = 
         repo.GetTimeRecords()
         |> Seq.filter (fun r -> r.Date >= startDate)
         |> Seq.map timeRecordToHoursRecord
      foundRecords
   
   member __.CurrentTime = 
      let now = DateTime.Now
      now.ToLongDateString() + " " + now.ToLongTimeString()
   
   member x.UpdateFor(instant : DateTime) = 
      if updateRepository instant then x.OnPropertyChanged "Hours"
