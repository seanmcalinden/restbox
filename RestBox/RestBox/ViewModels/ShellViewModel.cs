﻿using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Events;

namespace RestBox.ViewModels
{
    public class ShellViewModel : ViewModelBase<ShellViewModel>
    {
        #region Declarations

        private readonly IMainMenuApplicationService mainMenuApplicationService; 

        #endregion

        #region Constructor

        public ShellViewModel(
          IEventAggregator eventAggregator,
          IMainMenuApplicationService mainMenuApplicationService)
        {
            this.mainMenuApplicationService = mainMenuApplicationService;
            MenuItems = new ObservableCollection<MenuItem>();
            eventAggregator.GetEvent<NewSolutionEvent>().Subscribe(NewSolutionSetUp);
            eventAggregator.GetEvent<OpenSolutionEvent>().Subscribe(OpenSolution);
            eventAggregator.GetEvent<CloseSolutionEvent>().Subscribe(CloseSolution);
            eventAggregator.GetEvent<ResetMenuEvent>().Subscribe(ResetMenu);
            SolutionLoadedVisibility = Visibility.Visible;
        } 

        #endregion

        #region Properties

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

        #endregion

        #region Event Handlers

        private void ResetMenu(bool obj)
        {
            MenuItems.Clear();
            mainMenuApplicationService.CreateInitialMenuItems();
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
            MenuItems.Clear();
            mainMenuApplicationService.CreateInitialMenuItems();
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

        #endregion

        #region Command Handlers

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        } 

        #endregion
    }
}
