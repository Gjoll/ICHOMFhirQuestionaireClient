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
        Questionnaire? questionaire = null;
        Patient? patient = null;
        FhirClient? client = null;

        String baseUrl = String.Empty;
        String apiToken = String.Empty;
        String patientId = String.Empty;

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
            webView.CoreWebView2.OpenDevToolsWindow();
        }

        private void WebView_NavigationCompleted(object? sender,
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
        }

        private async void mnuLoadQuestionaire_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.client == null)
                    throw new Exception($"Connect must be called first");
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Multiselect = false;
                if (ofd.ShowDialog() != true)
                    return;
                dynamic obj = new ExpandoObject();
                obj.questionaire = File.ReadAllText(ofd.FileName);

                FhirJsonParser fjp = new FhirJsonParser();
                this.questionaire = fjp.Parse<Questionnaire>(obj.questionaire);

                String patientData = this.patient.ToJson();
                string data = JsonConvert.SerializeObject(obj);
                await this.webView.ExecuteScriptAsync($"loadQuestionaire({data}, {patientData})");
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

        private async void mnuStoreServer_Click(object sender, RoutedEventArgs e)
        {
            if (this.client == null)
                throw new Exception($"Connect must be called first");
            if (this.patient == null)
                throw new Exception($"Connect must be called first");
            if (this.questionaire == null)
                throw new Exception($"Load a questionaire first!");
            String results = await this.webView.ExecuteScriptAsync($"saveQuestionaire()");
            FhirJsonParser fjp = new FhirJsonParser();
            QuestionnaireResponse qResp = fjp.Parse<QuestionnaireResponse>(results);
            if (null == qResp)
                throw new Exception($"Error deserializing questionaire response");

            qResp.QuestionnaireElement = this.questionaire.Url;
            qResp.Meta.ProfileElement = null;
            qResp.Subject = new ResourceReference($"{patient.TypeName}/{patient.Id}");
            client.Create(qResp);
        }


        private void ConnectToServer(String baseUrl, String apiToken, String patientId)
        {
            this.baseUrl = baseUrl;
            this.apiToken = apiToken;
            this.patientId = patientId;
            AuthorizationMessageHandler? auth = null;
            if (String.IsNullOrEmpty(apiToken) == false)
                auth = new AuthorizationMessageHandler(apiToken);

            this.client = new FhirClient(this.baseUrl, new() { PreferredFormat = ResourceFormat.Json }, auth);
            this.patient = (Patient)client.Get($"{this.baseUrl}/Patient/{patientId}");
        }

        private void mnuConnectFirely_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ConnectToServer("http://server.fire.ly", String.Empty, "186c5ca5-a858-411a-8166-7b2c374e9f4b");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }


        private void mnuConnectAzureHealthCare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ConnectToServer(
                    "https://hristo-ouch.fhir.azurehealthcareapis.com",
                    "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiJodHRwczovL2hyaXN0by1vdWNoLmZoaXIuYXp1cmVoZWFsdGhjYXJlYXBpcy5jb20iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83NzRkZDg0OS04OGEyLTRjZDQtODc4My0yMjVhN2I3ZjVlMzcvIiwiaWF0IjoxNjYzNTI0Mzg3LCJuYmYiOjE2NjM1MjQzODcsImV4cCI6MTY2MzUyODI4NywiYWlvIjoiRTJaZ1lQamlMTG50YVhyNS9Ka2VTbCthWis0SkFRQT0iLCJhcHBpZCI6IjIxMGU5MmI1LWQ3NWYtNGUyZC04MzE3LWJjYzhmZjA4YTJiNyIsImFwcGlkYWNyIjoiMSIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0Lzc3NGRkODQ5LTg4YTItNGNkNC04NzgzLTIyNWE3YjdmNWUzNy8iLCJvaWQiOiIzNTAxMGQ3Yi0wYmI2LTRjYTctOTFlYy0yNjk0Yjc4NWJmOTIiLCJyaCI6IjAuQVRrQVNkaE5kNktJMUV5SGd5SmFlMzllTjloNFowX3ZXdHhEb2Ytd2MzSkxsSlU1QUFBLiIsInN1YiI6IjM1MDEwZDdiLTBiYjYtNGNhNy05MWVjLTI2OTRiNzg1YmY5MiIsInRpZCI6Ijc3NGRkODQ5LTg4YTItNGNkNC04NzgzLTIyNWE3YjdmNWUzNyIsInV0aSI6Il91WFl3VVVYUzBtbHNpRlBMQUI2QUEiLCJ2ZXIiOiIxLjAifQ.a782UcXe3Nk3Pkl4aigFmrBZ1Ylv__g6V13_EfQd-ehUvQE4DPS1PvjiyX8XSZgc2SceRVOAStm0nHunjIqyecXGt3hi2bjaV3kvO7affdq97EMYpXct4hLXjOYad-3Q22-sSMsCcmuHYsKcNspkXSsVl9T9kpdVviPSZ9Zhj5nkTH-9epGLl8Jb6OrBLlOLiIR3WdGABjb9RO_3VSY3q5NPVgKku35fiHa-ZNsl-9obUQ0mSaCR7IlcLUz7p7avjmixkD4qRIED0v65V0O7MqdeN5ulPKwIgGDbUGXXcCLaDYVaTZHN4rIS1yWREf7BWPel9WEW0I_HXMdimnSjzA",
                    "BreastCancerPatient147"
                    );
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
