using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.ServiceLocation;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;

namespace RestBox.ViewModels
{
    public class ShellViewModel : ViewModelBase<ShellViewModel>
    {
        #region Declarations

        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private IEventAggregator eventAggregator;

        #endregion

        #region Constructor

        public ShellViewModel(
          IEventAggregator eventAggregator,
          IMainMenuApplicationService mainMenuApplicationService)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.eventAggregator = eventAggregator;
            MenuItems = new ObservableCollection<MenuItem>();
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(NewSolutionSetUp);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(OpenSolution);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(CloseSolution);
            eventAggregator.GetEvent<UpdateToolBarEvent>().Subscribe(UpdateToolBar);
            SolutionLoadedVisibility = Visibility.Visible;
            eventAggregator.GetEvent<UpdateEnvironmentEvent>().Subscribe(UpdateSelectedEnvironment);
            SaveButtonVisibility = Visibility.Collapsed;
            RunButtonVisibility = Visibility.Collapsed;
        }

        #endregion

        #region Properties

        private Visibility saveButtonVisibility;
        public Visibility SaveButtonVisibility
        {
            get { return saveButtonVisibility; }
            set { saveButtonVisibility = value; OnPropertyChanged(x => x.SaveButtonVisibility); }
        }

        private Visibility runButtonVisibility;
        public Visibility RunButtonVisibility
        {
            get { return runButtonVisibility; }
            set { runButtonVisibility = value; OnPropertyChanged(x => x.RunButtonVisibility); }
        }

        private Visibility solutionLoadedVisibility;
        public Visibility SolutionLoadedVisibility
        {
            get { return solutionLoadedVisibility; }
            set { solutionLoadedVisibility = value; OnPropertyChanged(x => x.SolutionLoadedVisibility); }
        }

        private string applicationTitle;
        public string ApplicationTitle
        {
            get { return applicationTitle; }
            set { applicationTitle = value; OnPropertyChanged(x => x.ApplicationTitle); }
        }

        public ObservableCollection<MenuItem> MenuItems { get; private set; }

        public ObservableCollection<ViewFile> Environments
        {
            get { return ServiceLocator.Current.GetInstance<RequestEnvironmentsFilesViewModel>().ViewFiles; }
        }
        
        public ViewFile SelectedEnvironment
        {
            get { return ServiceLocator.Current.GetInstance<RequestEnvironmentsFilesViewModel>().Selected; }
            set { ServiceLocator.Current.GetInstance<RequestEnvironmentsFilesViewModel>().Selected = value; 
                OnPropertyChanged(x => x.SelectedEnvironment); }
        }

        #endregion

        #region Event Handlers

        private void UpdateToolBar(List<ToolBarItemData> toolBarItemDatas)
        {
            foreach (var toolBarItemData in toolBarItemDatas)
            {
                switch (toolBarItemData.ToolBarItemType)
                {
                    case ToolBarItemType.NewSolution:
                        NewSolutionCommand = toolBarItemData.Command;
                        break;
                    case ToolBarItemType.OpenSolution:
                        OpenSolutionCommand = toolBarItemData.Command;
                        break;
                    case ToolBarItemType.SaveAll:
                        SaveAllCommand = toolBarItemData.Command;
                        break;
                    case ToolBarItemType.Run:
                        RunCommand = toolBarItemData.Command;
                        RunButtonVisibility = toolBarItemData.Visibility;
                        break;
                    case ToolBarItemType.Save:
                        SaveCommand = toolBarItemData.Command;
                        SaveButtonVisibility = toolBarItemData.Visibility;
                        break;
                    case ToolBarItemType.Help:
                        HelpCommand = toolBarItemData.Command;
                        break;
                }
            }
        }

        private void UpdateSelectedEnvironment(bool obj)
        {
            OnPropertyChanged(x => x.SelectedEnvironment);
        }

        private void OpenSolution(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Visible;
            CloseSolution(true);
            ApplicationTitle = string.Format("REST Box - {0}", Solution.Current.Name);
        }

        private void NewSolutionSetUp(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Visible;
            CloseSolution(true);
            ApplicationTitle = string.Format("REST Box - {0}", Solution.Current.Name);
        }

        private void CloseSolution(bool obj)
        {
            SolutionLoadedVisibility = Visibility.Hidden;
        } 

        #endregion

        #region Commands

        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand(ExitApplication);
            }
        }

        private ICommand runCommand;
        public ICommand RunCommand
        {
            get { return runCommand; }
            set
            {
                runCommand = value;
                OnPropertyChanged(x => x.RunCommand);
            }
        }

        private ICommand saveCommand;
        public ICommand SaveCommand
        {
            get { return saveCommand; }
            set
            {
                saveCommand = value;
                OnPropertyChanged(x => x.SaveCommand);
            }
        }

        private ICommand newSolutionCommand;
        public ICommand NewSolutionCommand
        {
            get { return newSolutionCommand; }
            set
            {
                newSolutionCommand = value;
                OnPropertyChanged(x => x.NewSolutionCommand);
            }
        }

        private ICommand openSolutionCommand;
        public ICommand OpenSolutionCommand
        {
            get { return openSolutionCommand; }
            set
            {
                openSolutionCommand = value;
                OnPropertyChanged(x => x.OpenSolutionCommand);
            }
        }

        private ICommand saveAllCommand;
        public ICommand SaveAllCommand
        {
            get { return saveAllCommand; }
            set
            {
                saveAllCommand = value;
                OnPropertyChanged(x => x.SaveAllCommand);
            }
        }

        private ICommand helpCommand;
        public ICommand HelpCommand
        {
            get { return helpCommand; }
            set
            {
                helpCommand = value;
                OnPropertyChanged(x => x.HelpCommand);
            }
        }

        #endregion

        #region Command Handlers

        private void ExitApplication()
        {
            var shell = ServiceLocator.Current.GetInstance<Shell>();
            shell.OnClosing(shell, new CancelEventArgs(false), c => Application.Current.Shutdown());
        }

        public bool CloseSolutionLayoutDocuments()
        {
            var cancelled = false;
            var shell = ServiceLocator.Current.GetInstance<Shell>();
            shell.OnClosing(shell, new CancelEventArgs(false), c => cancelled = c);
            return cancelled;
        }

        #endregion
    }
}
