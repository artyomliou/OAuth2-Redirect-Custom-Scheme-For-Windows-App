// Copyright 2016 Google Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

namespace OAuthApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <see cref="https://github.com/googlesamples/oauth-apps-for-windows"/>
    /// <see cref="https://stackoverflow.com/a/48457204"/>
    public partial class MainWindow : Window
    {
        public OAuthState State { get; }
        private OAuthRequest request;

        public MainWindow()
        {
            InitializeComponent();
            State = new OAuthState();
            DataContext = this;
            Closing += MainWindow_Closing;
        }

        /// <summary>
        /// When clicked, should open browser (edge here) and navigate to login page
        /// </summary>
        private void Button_Click_Login(object sender, RoutedEventArgs e)
        {
            State.Token = null;
            request = null;

            var scopes = new string[] {
                "openid",
                "profile",
                "email",
                "phone"
            };

            request = OAuthRequest.BuildRequest(scopes);

            // Open login page in Microsoft Edge
            // (--inPrivate prevents from browser remembered last user)
            System.Diagnostics.Process.Start(new ProcessStartInfo()
            {
                FileName = "msedge.exe",
                Arguments = string.Format("--inPrivate --new-window --no-first-run \"{0}\"", request.AuthorizationRequestUri),
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Process OAuth callback
        /// </summary>
        internal async Task OauthCallbackAsync(string code = null, string state = null, string error = null)
        {
            // Checks for errors.
            if (error != null)
            {
                output(String.Format("OAuth authorization error: {0}.", error));
                return;
            }
            if (code == null || state == null)
            {
                output("Malformed authorization response. ");
                return;
            }

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (state != request.State)
            {
                output(String.Format("Received request with invalid state ({0})", state));
                return;
            }
            output("Authorization code: " + code);

            State.Token = await request.ExchangeCodeForAccessTokenAsync(code);
        }

        private void Button_Click_Logout(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logout();
        }

        /// <summary>
        /// Clear on-screen log, then revoke any derived access-token, and clear the OAuthState
        /// </summary>
        internal void Logout()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                textBoxOutput.Text = "";
            }));
            output("logout...");

            if (State.Token != null) {
                request.RevokeAsync(State.Token?.RefreshToken);
            }

            State.Token = null;
            request = null;
        }

        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="output">string to be appended</param>
        internal void output(string output)
        {
            // https://stackoverflow.com/a/34554362
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                textBoxOutput.Text = textBoxOutput.Text + output + Environment.NewLine;
            }));
            Console.WriteLine(output);
        }

        /// <summary>Brings main window to foreground.</summary>
        public void BringToForeground()
        {
            if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            }

            // According to some sources these steps gurantee that an app will be brought to foreground.
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }
    }
}