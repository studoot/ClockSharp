module MainApp

open System
open System.Windows
open System.Windows.Data
open ClockSharp.ViewModel
open ClockSharp.HoursRepository
open ClockSharp.HoursRepository.IO

// Create the View and bind it to the View Model
let mainWindow = 
   Application.LoadComponent(new System.Uri("/ClockSharp;component/mainwindow.xaml", UriKind.Relative)) :?> Window


// Application Entry point
[<STAThread>]
[<EntryPoint>]
let main (_) = 
   let repo = (LoadRepository (__SOURCE_DIRECTORY__ + @"\u404261.clock-card")).Value
   let records = repo.GetTimeRecords()
   let theMainWindow = mainWindow
   let viewModelProvider = (theMainWindow.Resources.["HoursViewModel"] :?> ObjectDataProvider)
   let viewModel = (viewModelProvider.ObjectInstance :?> Hours)
   viewModel.Repository <- repo
   (new Application()).Run(theMainWindow)
