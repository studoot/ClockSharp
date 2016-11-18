module private ClockSharp.HoursRepository.Text

open ClockSharp.HoursRepository.Interface
open ClockSharp.Model
open FSharpx
open FSharpx.Option
open System
open System.Globalization
open System.IO
open System.Text

let private recordLength = 30

type System.DateTime with
   static member ParseExact(s : string, format : string) = 
      let mutable value = Unchecked.defaultof<System.DateTime>
      ofBoolAndValue 
         (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, &value), value)

type System.IO.StreamReader with
   member this.ReadLines : string seq = 
      this.BaseStream.Position <- int64 0
      seq { 
         while not this.EndOfStream do
            yield this.ReadLine()
      }

let private dateOf (record : string) = DateTime.ParseExact(record.Substring(5, 11), "dd-MMM-yyyy")
let private startOf (record : string) = DateTime.ParseExact(record.Substring(17, 5), "HH:mm")
let private finishOf (record : string) = DateTime.ParseExact(record.Substring(23, 5), "HH:mm")

let private toTimeRecord d s f = 
   { Date = DateTimeToDate d
     Start = DateTimeToTimePoint s
     Finish = DateTimeToTimePoint f }

let private timeRecordFrom record = toTimeRecord <!> dateOf record <*> startOf record <*> finishOf record

let ReadRecord(s : Stream) : TimeRecord option = 
   try 
      let buffer : byte [] = Array.zeroCreate recordLength
      if recordLength = s.Read(buffer, 0, recordLength) then Encoding.ASCII.GetString buffer |> timeRecordFrom
      else None
   with _ -> None

let WriteRecord (s : Stream) (r : TimeRecord) : bool = 
   try 
      let day = (DateToDateTime r.Date).ToString("ddd, dd-MMM-yyyy")
      let start = (TimePointToTimeSpan r.Start).ToString(@"hh\:mm")
      let finish = (TimePointToTimeSpan r.Finish).ToString(@"hh\:mm")
      let newRecord = sprintf "%16s %5s %5s\x0d\x0a" day start finish
      let recordBytes = Encoding.ASCII.GetBytes newRecord
      if recordBytes.Length = recordLength then 
         s.Write(recordBytes, 0, recordLength)
         true
      else false
   with _ -> false

let LoadRecords(s : Stream) = 
   try 
      (new StreamReader(s)).ReadLines
      |> Seq.map timeRecordFrom
      |> Seq.toList
      |> Option.sequence
      |> Option.map Seq.ofList
   with e -> None

type TextHoursRepository(s : Stream) = 
   let recordsConstructor = fun () -> LoadRecords s |> getOrElse Seq.empty
   let resetRecords = fun () -> Lazy.Create recordsConstructor
   let mutable records: Lazy<TimeRecords> = resetRecords()
   new(path : string) = 
      new TextHoursRepository(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
   
   interface IDisposable with
      member this.Dispose() = s.Dispose()
   
   interface IHoursRepository with
      member this.GetTimeRecords() = records.Force()
      
      member this.Insert newRecords = 
         let existingRecords = (this :> IHoursRepository).GetTimeRecords()
         let allRecords = Seq.append existingRecords newRecords
         let distinctRecordCount = Seq.distinctBy (fun r -> r.Date) allRecords |> Seq.length
         if distinctRecordCount = Seq.length allRecords then 
            let sortedRecords = Seq.sortBy (fun r -> r.Date) allRecords
            s.Position <- 0L
            Seq.forall (WriteRecord s) sortedRecords |> ignore
            s.Flush()
            records <- resetRecords()
         this :> IHoursRepository |> Some
      
      member this.Update (d : Date) (t : TimePoint) = 
         try 
            let existingRecords = (this :> IHoursRepository).GetTimeRecords()
            let recordIndex = Seq.findIndex (fun r -> r.Date = d) existingRecords
            s.Position <- int64 recordLength * int64 recordIndex
            match ReadRecord s with
            | Some record ->
               let newRecord = { record with Finish = t }
               s.Position <- int64 recordLength * int64 recordIndex
               WriteRecord s newRecord |> ignore
               s.Flush()
            | _ -> ()
            records <- resetRecords()
            this :> IHoursRepository |> Some
         with _ -> (this :> IHoursRepository).Insert [{ Date = d
                                                        Start = t
                                                        Finish = t }
 ]

let Load(path : string) = new TextHoursRepository(path) :> IHoursRepository |> Some
