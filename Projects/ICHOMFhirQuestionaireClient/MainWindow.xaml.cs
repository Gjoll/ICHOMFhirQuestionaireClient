using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
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

        private async void mnuLoadQuestionaire_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Multiselect = false;
                if (ofd.ShowDialog() != true)
                    return;
                dynamic obj = new ExpandoObject();
                obj.questionaire = File.ReadAllText(ofd.FileName);
                string data = JsonConvert.SerializeObject(obj);
                await this.webView.ExecuteScriptAsync($"loadQuestionaire({data})");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        //private async void mnuSaveQuestionaire_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        String results = await this.webView.ExecuteScriptAsync($"saveQuestionaire()");
        //        FhirJsonParser fjp = new FhirJsonParser();
        //        QuestionnaireResponse qResp = fjp.Parse<QuestionnaireResponse>(results);
        //        if (null == qResp)
        //            throw new Exception($"Error deserializing questionaire response");
        //        var client = new FhirClient("http://server.fire.ly");
        //        client.Create(qResp);
        //    }
        //    catch (Exception err)
        //    {
        //        MessageBox.Show(err.Message);
        //    }
        //}

        private async void mnuSaveQuestionaire_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                String results = await this.webView.ExecuteScriptAsync($"saveQuestionaire()");
                SaveFileDialog ofd = new SaveFileDialog();
                ofd.DefaultExt = ".json";
                if (ofd.ShowDialog() != true)
                    return;
                await File.WriteAllTextAsync(ofd.FileName, results);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        
    }
}
