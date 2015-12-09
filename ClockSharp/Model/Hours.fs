module ClockSharp.Model

open System

[<Measure>]
type hrs

[<Measure>]
type min

[<Measure>]
type y

[<Measure>]
type m

[<Measure>]
type d

type TimePoint = int<hrs> * int<min>

let AsTimePoint(t : DateTime) : TimePoint = (t.Hour * 1<hrs>, 1<min> * t.Minute)

let TimePointToDateTime(t : TimePoint) : DateTime = 
   match t with
   | (hours, mins) -> new DateTime(int hours, int mins, 0)

type Date = int<y> * int<m> * int<d>

let DateToDateTime(d : Date) : DateTime = 
   match d with
   | (year, month, day) -> new DateTime(int year, int month, int day)

let AsDate(d : DateTime) : Date = (d.Year * 1<y>, d.Month * 1<m>, d.Day * 1<d>)

type TimeRecord = 
   { Date : Date
     Start : TimePoint
     Finish : TimePoint }

type TimeRecords = TimeRecord seq
