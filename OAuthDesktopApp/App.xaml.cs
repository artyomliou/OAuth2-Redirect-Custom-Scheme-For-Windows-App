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

using Microsoft.Win32;
using System;
using System.Web;
using System.Windows;

namespace OAuthApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region custom scheme
        public const string UriScheme = "myapps";
        public const string UriHost = "host";
        public const string FriendlyName = "myapp friendly name";
        #endregion
        #region singleton
        private readonly Guid guid = new Guid("{any string you want}");
        #endregion

        private void AppOnStartup(object sender, StartupEventArgs e)
        {
            var singleInstance = new SingleInstance(guid);
            if (singleInstance.IsFirstInstance)
            {
                Console.WriteLine("is first instance");
                singleInstance.ArgumentsReceived += SingleInstanceParameter;
                singleInstance.ListenForArgumentsFromSuccessiveInstances();

                // Do your other app logic
                RegisterUriScheme();
            }
            else
            {
                Console.WriteLine("is successive instance");
                // if there is an argument available, fire it
                if (e.Args.Length > 0)
                {
                    singleInstance.PassArgumentsToFirstInstance(e.Args[0]);
                }

                Environment.Exit(0);
            }
        }

        static void SingleInstanceParameter(object sender, GenericEventArgs<string> e)
        {
            if (! Uri.TryCreate(e.Data, UriKind.Absolute, out var uri))
            {
                return;
            }
            if (uri == null) {
                return;
            }
            if (! string.Equals(uri.Scheme, UriScheme, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (uri.Host != UriHost) {
                return;
            }
            switch (uri.LocalPath) {
                case "/oauth2/callback":
                    Current.Dispatcher.BeginInvoke(
                            (Action)(() => {
                                // Bring window to foreground
                                var wnd = (MainWindow)Current.MainWindow;
                                wnd.BringToForeground();

                                // Callback function
                                var q = HttpUtility.ParseQueryString(uri.Query);
                                wnd.OauthCallbackAsync(q.Get("code"), q.Get("state"), q.Get("error"));
                            }));
                    break;
            }
        }

        /// <see cref="https://www.meziantou.net/registering-an-application-to-a-uri-scheme-using-net.htm"/>
        private static void RegisterUriScheme()
        {
            using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme))
            {
                // Replace typeof(App) by the class that contains the Main method or any class located in the project that produces the exe.
                // or replace typeof(App).Assembly.Location by anything that gives the full path to the exe
                string applicationLocation = typeof(App).Assembly.Location;

                key.SetValue("", "URL:" + FriendlyName);
                key.SetValue("URL Protocol", "");

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                {
                    defaultIcon.SetValue("", applicationLocation + ",1");
                }

                using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
                }
            }
        }
    }
}
