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
using System.Reflection;
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
        Questionnaire? loadedQuestionaire = null;

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
            //webView.CoreWebView2.OpenDevToolsWindow();

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

                FhirJsonParser fjp = new FhirJsonParser();
                this.loadedQuestionaire = fjp.Parse<Questionnaire>(obj.questionaire);

                string data = JsonConvert.SerializeObject(obj);
                await this.webView.ExecuteScriptAsync($"loadQuestionaire({data})");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private async System.Threading.Tasks.Task Store(String baseStoreUrl, String apiToken)
        {
            if (this.loadedQuestionaire == null)
                throw new Exception($"Load a questionaire first!");
            String results = await this.webView.ExecuteScriptAsync($"saveQuestionaire()");
            FhirJsonParser fjp = new FhirJsonParser();
            QuestionnaireResponse qResp = fjp.Parse<QuestionnaireResponse>(results);
            if (null == qResp)
                throw new Exception($"Error deserializing questionaire response");

            var client = new FhirClient($"{baseStoreUrl}");
            if (String.IsNullOrEmpty(apiToken) == false)
            {
                // client.`.AddHeader("Authorization", $"Bearer {this.ApiToken}");
            }

            Bundle patients = (Bundle)client.Get($"{baseStoreUrl}//Patient?_count=30&sort=_lastUpdated");
            if (patients.Entry.Count == 0)
                throw new Exception($"No patients loaded from firely!");
            qResp.QuestionnaireElement = this.loadedQuestionaire.Url;
            qResp.Meta.ProfileElement = null;
            Patient patientEntry = (Patient)patients.Entry[0].Resource;
            qResp.Subject = new ResourceReference($"{patientEntry.TypeName}/{patientEntry.Id}");
            client.Create(qResp);
        }

        private async void mnuLoadStoreAzureHealthCare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Store("", String.Empty);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private async void mnuUpdateQuestionaires_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Multiselect = false;
                if (ofd.ShowDialog() != true)
                    return;

                FhirJsonParser fjp = new FhirJsonParser();
                Bundle items = (Bundle)await fjp.ParseAsync(File.ReadAllText(ofd.FileName));

                //Fixme Hard coded path!
                String baseDir = @"D:\Development\HL7\ICHOMFhirQuestionaireClient\Projects\ICHOMFhirQuestionaireClient\Questionnaires";
                foreach (var entry in items.Entry)
                {
                    Questionnaire? questionnaire = entry.Resource as Questionnaire;
                    if (questionnaire != null)
                    {
                        String test = await questionnaire.ToJsonAsync();
                        File.WriteAllText(Path.Combine(baseDir, $"{questionnaire.Name}.json"), test);
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private async void mnuLoadStoreFirely_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Store("http://server.fire.ly", String.Empty);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

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
