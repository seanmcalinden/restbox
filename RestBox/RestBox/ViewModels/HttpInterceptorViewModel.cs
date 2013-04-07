using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Win32;
using RestBox.ApplicationServices;
using RestBox.Domain.Entities;
using RestBox.Domain.Services;
using RestBox.Events;
using RestBox.UserControls;
using RestBox.Utilities;

namespace RestBox.ViewModels
{
    public class HttpInterceptorViewModel : ViewModelBase<HttpInterceptorViewModel>, ISave
    {
        private IEventAggregator eventAggregator;
        private readonly IMainMenuApplicationService mainMenuApplicationService;
        private readonly IFileService fileService;
        private readonly IJsonSerializer jsonSerializer;
        private readonly IProxyService proxyService;
        private readonly KeyGesture saveKeyGesture;
        private readonly KeyGesture runHttpRequestKeyGesture; 

        public HttpInterceptorViewModel(IEventAggregator eventAggregator, IMainMenuApplicationService mainMenuApplicationService, IFileService fileService, IJsonSerializer jsonSerializer, IProxyService proxyService)
        {
            this.eventAggregator = eventAggregator;
            this.mainMenuApplicationService = mainMenuApplicationService;
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
            this.proxyService = proxyService;

            saveKeyGesture = new KeyGesture(Key.S, ModifierKeys.Control);
            runHttpRequestKeyGesture = new KeyGesture(Key.F5);

            Verbs = new ObservableCollection<ComboBoxItem>
                               {
                                   new ComboBoxItem {Content = "GET", IsSelected = true},
                                   new ComboBoxItem {Content = "POST"},
                                   new ComboBoxItem {Content = "PUT"},
                                   new ComboBoxItem {Content = "DELETE"},
                                   new ComboBoxItem {Content = "OPTIONS"},
                                   new ComboBoxItem {Content = "HEAD"},
                                   new ComboBoxItem {Content = "TRACE"}
                               };
            Verb = Verbs[0];
            ProgressBarVisibility = Visibility.Hidden;
            StartButtonVisibility = Visibility.Visible;
            CancelButtonVisibility = Visibility.Hidden;
            eventAggregator.GetEvent<AddHttpInterceptorMenuItemsEvent>().Subscribe(AddHttpInterceptorMenuItems);
        }

        public ObservableCollection<ComboBoxItem> Verbs { get; set; }

        private string url;
        public string Url
        {
            get { return url; }
            set
            {
                url = value;
                eventAggregator.GetEvent<UpdateInterceptorUrlEvent>().Publish(this);
                OnPropertyChanged(x => x.Url);
            }
        }

        public void SetUrl(string urlField)
        {
            url = urlField;
        }

        private ComboBoxItem verb;
        public ComboBoxItem Verb
        {
            get { return verb; }
            set
            {
                verb = value;
                OnPropertyChanged(x => x.Verb);
                VerbString = value.Content.ToString();
            }
        }

        public string VerbString { get; set; }

        private string headers;
        public string Headers
        {
            get { return headers; }
            set
            {
                headers = value;
                eventAggregator.GetEvent<UpdateInterceptorHeadersEvent>().Publish(this);
                OnPropertyChanged(x => x.Headers);
            }
        }

        public void SetRequestHeaders(string headersField)
        {
            headers = headersField;
        }

        private string body;
        public string Body
        {
            get { return body; }
            set
            {
                body = value;
                eventAggregator.GetEvent<UpdateInterceptorBodyEvent>().Publish(this);
                OnPropertyChanged(x => x.Body);
            }
        }

        public void SetRequestBody(string bodyField)
        {
            body = bodyField;
        }

        private void AddHttpInterceptorMenuItems(HttpInterceptorViewModel httpInterceptorViewModel)
        {
            if (httpInterceptorViewModel != this)
            {
                return;
            }

            eventAggregator.GetEvent<RemoveInputBindingEvent>().Publish(true);

            var fileMenu = mainMenuApplicationService.Get("file");
            var saveHttpRequest = new MenuItem { Header = "Save Http Request", InputGestureText = "Ctrl+S" };
            var saveHttpRequestAs = new MenuItem { Header = "Save Http Request As..." };

            saveHttpRequest.Command = new DelegateCommand(SetupSaveRequest);
            saveHttpRequestAs.Command = new DelegateCommand(SetupSaveRequestAs);

            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequest, 3);
            mainMenuApplicationService.InsertMenuItem(fileMenu, saveHttpRequestAs, 4);

            var requests = mainMenuApplicationService.Get("interceptors");
            var startInterceptor = mainMenuApplicationService.GetChild(requests, "interceptorsStart");
            startInterceptor.Command = StartInterceptorCommand;
            startInterceptor.IsEnabled = true;

            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = saveKeyGesture, Command = saveHttpRequest.Command });

            eventAggregator.GetEvent<AddInputBindingEvent>().Publish(new KeyBindingData { KeyGesture = runHttpRequestKeyGesture, Command = startInterceptor.Command });
            eventAggregator.GetEvent<UpdateToolBarEvent>().Publish(new List<ToolBarItemData>
                {
                    new ToolBarItemData{ Command = saveHttpRequest.Command, Visibility = Visibility.Visible,ToolBarItemType = ToolBarItemType.Save},
                    new ToolBarItemData{ Command = startInterceptor.Command, Visibility = Visibility.Visible, ToolBarItemType = ToolBarItemType.Run}
                });
        }

        public ICommand StopInterceptorCommand
        {
            get { return new DelegateCommand(StopInterceptor); }
        }

        private void StopInterceptor()
        {
            //proxyService.RemoveInterceptor();
            StartButtonVisibility = Visibility.Visible;
            CancelButtonVisibility = Visibility.Hidden;
            ProgressBarVisibility = Visibility.Hidden;
        }

        public ICommand StartInterceptorCommand
        {
            get { return new DelegateCommand(StartInterceptor); }
        }

        private void StartInterceptor()
        {
            StartButtonVisibility = Visibility.Hidden;
            CancelButtonVisibility = Visibility.Visible;
            ProgressBarVisibility = Visibility.Visible;
            
            proxyService.AddInterceptor(new HttpRequestItem
                {
                    Url = url.Replace(Environment.NewLine, string.Empty),
                    Headers = headers,
                    Body = body,
                    Verb = VerbString
                });
            proxyService.Start();

        }

        private Visibility progressBarVisibility;

        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set
            {
                progressBarVisibility = value;
                OnPropertyChanged(x => x.ProgressBarVisibility);
            }
        }

        private bool isProgressBarEnabled;

        public bool IsProgressBarEnabled
        {
            get { return isProgressBarEnabled; }
            set
            {
                isProgressBarEnabled = value;
                OnPropertyChanged(x => x.IsProgressBarEnabled);
                if (value)
                {
                    ProgressBarVisibility = Visibility.Visible;
                }
                else
                {
                    ProgressBarVisibility = Visibility.Hidden;
                }
            }
        }

        private Visibility startButtonVisibility;
        public Visibility StartButtonVisibility
        {
            get { return startButtonVisibility; }
            set
            {
                startButtonVisibility = value;
                OnPropertyChanged(x => x.StartButtonVisibility);
            }
        }

        private Visibility cancelButtonVisibility;
        public Visibility CancelButtonVisibility
        {
            get { return cancelButtonVisibility; }
            set
            {
                cancelButtonVisibility = value;
                OnPropertyChanged(x => x.CancelButtonVisibility);
            }
        }


        private void SetupSaveRequestAs()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = SaveAs,
                UserControlType = typeof(HttpInterceptor),
                DataContext = this
            });
        }

        private void SetupSaveRequest()
        {
            eventAggregator.GetEvent<GetLayoutDataEvent>().Publish(new LayoutDataRequest
            {
                Action = Save,
                UserControlType = typeof(HttpInterceptor),
                DataContext = this
            });
        }

        public void SaveAs(string id, object content)
        {
            if (((HttpInterceptor) content).DataContext != this)
            {
                return;
            }

            var httpInterceptor = content as HttpInterceptor;

            var httpInterceptorViewModel = httpInterceptor.DataContext as HttpInterceptorViewModel;

            var saveFileDialog = new SaveFileDialog
                {
                    Filter = SystemFileTypes.Interceptor.FilterText,
                    FileName =
                        Path.GetFileName(httpInterceptor.FilePath) ?? SystemFileTypes.Interceptor.UntitledFileName,
                    Title = SystemFileTypes.Interceptor.SaveAsTitle
                };

            if (saveFileDialog.ShowDialog() == true)
            {
                var title = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                var httpFile = new HttpRequestItemFile
                    {
                        Url = httpInterceptorViewModel.url,
                        Verb = httpInterceptorViewModel.Verb.Content.ToString(),
                        Headers = httpInterceptorViewModel.Headers,
                        Body = httpInterceptorViewModel.Body
                    };

                var relativePath = fileService.GetRelativePath(new Uri(Solution.Current.FilePath),
                                                               saveFileDialog.FileName);


                var requestExists = Solution.Current.HttpInterceptorFiles.Any(x => x.Id == id);
                if (!requestExists)
                {
                    Solution.Current.HttpInterceptorFiles.Add(
                        new File {Id = id, RelativeFilePath = relativePath, Name = title});
                }
                else
                {
                    var existingInterceptorFile =
                        Solution.Current.HttpInterceptorFiles.First(x => x.Id == id);
                    existingInterceptorFile.Name = title;
                    existingInterceptorFile.RelativeFilePath = relativePath;
                }

                fileService.SaveSolution();

                fileService.SaveFile(saveFileDialog.FileName, jsonSerializer.ToJsonString(httpFile));

                httpInterceptor.FilePath = relativePath;
                eventAggregator.GetEvent<UpdateTabTitleEvent>().Publish(new TabHeader
                    {
                        Id = id,
                        Title = title
                    });
                eventAggregator.GetEvent<UpdateHttpInterceptorFileItemEvent>().Publish(new File
                    {
                        Id = id,
                        Name = title,
                        RelativeFilePath =
                            relativePath
                    });
            
            Keyboard.ClearFocus();
            eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(this, false));
            IsDirty = false;
            }

        }

        public void Save(string id, object content)
        {
            if (!(content is HttpInterceptor))
            {
                return;
            }

            if (((HttpInterceptor)content).DataContext != this)
            {
                return;
            }


            var httpInterceptor = content as HttpInterceptor;

            var httpInterceptorViewModel = httpInterceptor.DataContext as HttpInterceptorViewModel;

            if (string.IsNullOrWhiteSpace(httpInterceptor.FilePath))
            {
                SaveAs(id, content);
                return;
            }

            var httpFile = new HttpRequestItemFile
            {
                Url = httpInterceptorViewModel.Url,
                Verb = httpInterceptorViewModel.Verb.Content.ToString(),
                Headers = httpInterceptorViewModel.Headers,
                Body = httpInterceptorViewModel.Body
            };

            string filePath;

            if (!id.StartsWith("StandaloneNewItem"))
            {
                filePath = fileService.GetFilePath(Solution.Current.FilePath, httpInterceptor.FilePath);
            }
            else
            {
                filePath = httpInterceptor.FilePath;
            }

            fileService.SaveFile(filePath, jsonSerializer.ToJsonString(httpFile));

            eventAggregator.GetEvent<IsDirtyEvent>().Publish(new IsDirtyData(this, false));
            Keyboard.ClearFocus();
            httpInterceptorViewModel.IsDirty = false;
        }

        public bool IsDirty { get; set; }
    }
}
