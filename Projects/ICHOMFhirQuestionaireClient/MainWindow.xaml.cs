using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Rest.Legacy;
using Hl7.Fhir.Serialization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
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

        public class AuthorizationMessageHandler : HttpClientHandler
        {
            AuthenticationHeaderValue? authorization;

            public AuthorizationMessageHandler(String bearerToken)
            {
                authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }


            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Authorization = authorization;
                return base.SendAsync(request, cancellationToken);
            }
        }

        private async System.Threading.Tasks.Task Store(String baseStoreUrl,
            String patientId,
            String apiToken)
        {
            if (this.loadedQuestionaire == null)
                throw new Exception($"Load a questionaire first!");
            String results = await this.webView.ExecuteScriptAsync($"saveQuestionaire()");
            FhirJsonParser fjp = new FhirJsonParser();
            QuestionnaireResponse qResp = fjp.Parse<QuestionnaireResponse>(results);
            if (null == qResp)
                throw new Exception($"Error deserializing questionaire response");

            AuthorizationMessageHandler auth = null;
            if (String.IsNullOrEmpty(apiToken) == false)
                auth = new AuthorizationMessageHandler(apiToken);
            var client = new FhirClient(baseStoreUrl, new() { PreferredFormat = ResourceFormat.Json }, auth);

            Patient patient = (Patient)client.Get($"{baseStoreUrl}/Patient/{patientId}");
            //var patient = client.Read<Patient>(patientId);

            if (patient == null)
                throw new Exception($"Patients not loaded");
            qResp.QuestionnaireElement = this.loadedQuestionaire.Url;
            qResp.Meta.ProfileElement = null;
            qResp.Subject = new ResourceReference($"{patient.TypeName}/{patient.Id}");
            client.Create(qResp);
        }

        private async void mnuLoadStoreAzureHealthCare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                String baseUrl =
                    "https://hristo-ouch.fhir.azurehealthcareapis.com";
                String apiToken =
                    "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiJodHRwczovL2hyaXN0by1vdWNoLmZoaXIuYXp1cmVoZWFsdGhjYXJlYXBpcy5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83NzRkZDg0OS04OGEyLTRjZDQtODc4My0yMjVhN2I3ZjVlMzcvIiwiaWF0IjoxNjYzNDQxNzg2LCJuYmYiOjE2NjM0NDE3ODYsImV4cCI6MTY2MzQ0NTY4NiwiYWlvIjoiRTJaZ1lGaXIxUGJzWDdMUi9MbHNpZEhmeTdvWUFBPT0iLCJhcHBpZCI6IjIxMGU5MmI1LWQ3NWYtNGUyZC04MzE3LWJjYzhmZjA4YTJiNyIsImFwcGlkYWNyIjoiMSIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0Lzc3NGRkODQ5LTg4YTItNGNkNC04NzgzLTIyNWE3YjdmNWUzNy8iLCJvaWQiOiIzNTAxMGQ3Yi0wYmI2LTRjYTctOTFlYy0yNjk0Yjc4NWJmOTIiLCJyaCI6IjAuQVRrQVNkaE5kNktJMUV5SGd5SmFlMzllTjloNFowX3ZXdHhEb2Ytd2MzSkxsSlU1QUFBLiIsInN1YiI6IjM1MDEwZDdiLTBiYjYtNGNhNy05MWVjLTI2OTRiNzg1YmY5MiIsInRpZCI6Ijc3NGRkODQ5LTg4YTItNGNkNC04NzgzLTIyNWE3YjdmNWUzNyIsInV0aSI6Il9FajVoMnhHamstMHA1VGhkaEIwQUEiLCJ2ZXIiOiIxLjAifQ.XJf8o5xjeTwqckg4O62y-mQo_jnecxf1U070iUqvdfwKMLwNmbZPO6Ti_K8o4_28WclUZjXpm7vWBOoiF9BUzKJqB0cBVvxue5hdmlj-h7YhBrE0zjKBDq1eAnazew4pRaQGK2seOFkKnVDXuppUN2idVqofH_ToIWFP42yNatMEfhb6fQZryFlsfyB16UcNbTXeWtoaZCtT5f1VQ3RS8-uJNO0LIZY_HgUPLGugtHL1rQrt4yX4bzq6DLTjmkrzYzRM4se76wZQ_Je1VpaxXNhOwMRvfsMQOMQuqcA8ANo_PXKlPWNCQ9RHnbI7lZk_QQLc2l7dqNxbaBmSnzDUKw";
                String patientId = "BreastCancerPatient121";
                await Store(baseUrl, patientId, apiToken);
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
                foreach (var entry in items.Entry)
                {
                    {
                        String baseDir = @"D:\Development\HL7\ICHOMFhirQuestionaireClient\Projects\ICHOMFhirQuestionaireClient\Questionnaires";
                        Questionnaire? questionnaire = entry.Resource as Questionnaire;
                        if (questionnaire != null)
                        {
                            String test = await questionnaire.ToJsonAsync();
                            File.WriteAllText(Path.Combine(baseDir, $"{questionnaire.Name}.json"), test);
                        }
                    }
                    {
                        String baseDir = @"D:\Development\HL7\ICHOMFhirQuestionaireClient\Projects\ICHOMFhirQuestionaireClient\Patients";
                        Patient? patient = entry.Resource as Patient;
                        if (patient != null)
                        {
                            String test = await patient.ToJsonAsync();
                            File.WriteAllText(Path.Combine(baseDir, $"{patient.Name}.json"), test);
                        }
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
                await Store("http://server.fire.ly", "186c5ca5-a858-411a-8166-7b2c374e9f4b", String.Empty);
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
