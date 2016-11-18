module ClockSharp.Model

open System

[<Measure>]
type hr

[<Measure>]
type min

[<Measure>]
type y

[<Measure>]
type m

[<Measure>]
type d

type TimePoint = int<hr> * int<min>
type Date = int<y> * int<m> * int<d>

let Minutes (h:int<hr>) = h * 60<min/hr>

let StandardDay = (int (Minutes 7<hr>)) + 36

let TimeDiff (start : TimePoint) (finish : TimePoint) = 
   let hoursDiff = fst finish - fst start
   (snd finish - snd start) + (hoursDiff * 60<min/hr>)

let TimeSpanToTimePoint(t : TimeSpan) : TimePoint = (t.Hours * 1<hr>, t.Minutes * 1<min>)
let DateTimeToTimePoint(t : DateTime) : TimePoint = (t.Hour * 1<hr>, t.Minute * 1<min>)

let TimePointToTimeSpan(t : TimePoint) : TimeSpan = 
   match t with
   | (hours, mins) -> new TimeSpan(int hours, int mins, 0)

let TimePointToDateTime (d:Date) (t : TimePoint) : DateTime = 
   match (d, t) with
   | ((year, month, day), (hours, mins)) -> new DateTime(int year, int month, int day, int hours, int mins, 0)

let DateToDateTime(d : Date) : DateTime = 
   match d with
   | (year, month, day) -> new DateTime(int year, int month, int day)

let DateTimeToDate(d : DateTime) : Date = (d.Year * 1<y>, d.Month * 1<m>, d.Day * 1<d>)

type TimeRecord = 
   { Date : Date
     Start : TimePoint
     Finish : TimePoint }

let MinutesWorked (t:TimeRecord) = max 0 (int (TimeDiff t.Start t.Finish) - 30)

let DateTimeToTimeRecord instant = 
   { Date = DateTimeToDate instant
     Start = DateTimeToTimePoint instant
     Finish = DateTimeToTimePoint instant }

type TimeRecords = TimeRecord seq

let TimeRecordForDate (d : DateTime) (records : TimeRecords) = 
   let asDate = DateTimeToDate d
   let matchingRecords = Seq.filter (fun r -> (Date.Equals(r.Date, asDate))) records
   match Seq.length matchingRecords with
   | 1 -> Some(Seq.exactlyOne matchingRecords)
   | _ -> None
