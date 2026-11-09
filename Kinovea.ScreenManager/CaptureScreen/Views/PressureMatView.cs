using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Kinovea.ScreenManager
{
    internal sealed class PressureMatView : UserControl
    {
        private static readonly Uri dashboard = new Uri("http://127.0.0.1:8080/?embed=kinovea");
        private readonly WebView2 browser;
        private readonly Label status;
        private readonly Button retry;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(PressureMatView));

        public PressureMatView()
        {
            browser = new WebView2 { Dock = DockStyle.Fill, Visible = false };
            browser.NavigationCompleted += OnNavigationCompleted;

            status = new Label { AutoSize = true, MaximumSize = new Size(280, 0) };
            retry = new Button { AutoSize = true, Text = "Retry" };
            retry.Click += (sender, args) => LoadDashboard();
            var placeholder = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(8)
            };
            placeholder.Controls.Add(status);
            placeholder.Controls.Add(retry);
            Controls.Add(browser);
            Controls.Add(placeholder);
        }

        public async void LoadDashboard()
        {
            retry.Enabled = false;
            status.Text = "Loading pressure mat dashboard...";
            try
            {
                if (browser.CoreWebView2 == null)
                {
                    string userData = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Kinovea", "PressureMatWebView");
                    CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
                    if (IsDisposed || Disposing)
                        return;

                    await browser.EnsureCoreWebView2Async(environment);
                    if (IsDisposed || Disposing)
                        return;

                    browser.CoreWebView2.Settings.IsWebMessageEnabled = false;
                    browser.CoreWebView2.NavigationStarting += (sender, args) =>
                    {
                        if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out Uri uri) ||
                            uri.GetLeftPart(UriPartial.Authority) != dashboard.GetLeftPart(UriPartial.Authority))
                            args.Cancel = true;
                    };
                    browser.CoreWebView2.NewWindowRequested += (sender, args) => args.Handled = true;
                }

                browser.CoreWebView2.Navigate(dashboard.AbsoluteUri);
            }
            catch (Exception exception)
            {
                if (IsDisposed || Disposing)
                    return;

                log.Warn("Could not initialize the pressure mat dashboard.", exception);
                status.Text = "Could not open the dashboard. Check that the Microsoft Edge WebView2 Runtime is installed.";
                retry.Enabled = true;
            }
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                status.Parent.Visible = false;
                browser.Visible = true;
                return;
            }

            browser.Visible = false;
            status.Parent.Visible = true;
            status.Text = "Start the pressure mat backend on this computer, then click Retry.";
            retry.Enabled = true;
        }
    }
}
