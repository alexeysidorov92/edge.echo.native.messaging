using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace EdgeEchoNativeMessaging
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private int edgeConnectionIndex = 0;
        private Dictionary<int, AppServiceConnection> edgeConnections = new Dictionary<int, AppServiceConnection>();
        private Dictionary<int, BackgroundTaskDeferral> edgeAppServiceDeferrals = new Dictionary<int, BackgroundTaskDeferral>();
        private StreamWriter writer = null;
        private object thisLock = new object();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            var stream = new FileStream(Path.Combine(Path.GetTempPath(), "kp.uwp.log"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            this.writer = new StreamWriter(stream);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Initializes the app service on the host process 
        /// </summary>
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            var taskInstance = args.TaskInstance;

            if (taskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;

                taskInstance.Canceled += OnEdgeAppServiceCanceled; // Associate a cancellation handler with the background task.
                var edgeAppServiceDeferral = taskInstance.GetDeferral(); // Get a deferral so that the service isn't terminated.
                var edgeConnection = appService.AppServiceConnection;
                edgeConnection.RequestReceived += OnEdgeAppServiceRequestReceived;
                edgeConnection.ServiceClosed += OnEdgeAppServiceClosed;

                lock (thisLock)
                {
                    edgeConnection.AppServiceName = edgeConnectionIndex.ToString();
                    edgeConnections.Add(edgeConnectionIndex, edgeConnection);
                    edgeAppServiceDeferrals.Add(edgeConnectionIndex, edgeAppServiceDeferral);
                    ++edgeConnectionIndex;
                }
            }
        }

        /// <summary>
        /// Receives message from Extension (via Edge)
        /// </summary>
        private async void OnEdgeAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();

            try
            {
                //writer.Write(args.Request.Message.First().ToString());
                //writer.Write('\n');
                //writer.Flush();
                await args.Request.SendResponseAsync(args.Request.Message);
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        /// <summary>
        /// Associate the cancellation handler with the background task 
        /// </summary>
        private void OnEdgeAppServiceCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            var appService = sender.TriggerDetails as AppServiceTriggerDetails;
            var index = Int32.Parse(appService.AppServiceConnection.AppServiceName);
            CloseConnection(index);
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private void OnEdgeAppServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            var index = Int32.Parse(sender.AppServiceName);
            CloseConnection(index);
        }

        private void CloseConnection(int index)
        {
            lock (thisLock)
            {
                if (edgeConnections.ContainsKey(index))
                {
                    var connection = edgeConnections[index];
                    edgeConnections.Remove(index);
                    connection?.Dispose();
                }

                if (edgeAppServiceDeferrals.ContainsKey(index))
                {
                    var deferral = edgeAppServiceDeferrals[index];
                    edgeAppServiceDeferrals.Remove(index);
                    deferral?.Complete();
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
