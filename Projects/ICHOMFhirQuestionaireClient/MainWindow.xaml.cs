using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace ICHOMFhirQuestionaireClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool firstLoad = true;

        public MainWindow()
        {
            InitializeComponent();
            string curDir = Directory.GetCurrentDirectory();
            InitializeAsync();
        }

        // called from Window Constructor after InitializeComponent()
        // note: the `async void` signature is required for environment init
        async void InitializeAsync()
        {
            // must create a data folder if running out of a secured folder that can't write like Program Files
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: Path.Combine(Path.GetTempPath(), "MarkdownMonster_Browser"));

            // NOTE: this waits until the first page is navigated - then continues
            //       executing the next line of code!
            await webView.EnsureCoreWebView2Async(env);

            //if (Model.Options.AutoOpenDevTools)
            webView.CoreWebView2.OpenDevToolsWindow();

            // Almost always need this event for something    
            webView.NavigationCompleted += WebView_NavigationCompleted;

            // set the initial URL
            webView.Source = new Uri(
                Path.Combine(Environment.CurrentDirectory,
                            "WebPages",
                            "index.html"));
        }

        private void WebView_NavigationCompleted(object? sender, 
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
        }
    }
}
