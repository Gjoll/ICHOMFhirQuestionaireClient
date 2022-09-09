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

namespace ICHOMFhirQuestionaireClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string curDir = Directory.GetCurrentDirectory();
            //wbMain.Source = $"file://127.0.0.1/c$/path/to/the/file";
            wbMain.Navigate(new Uri($"file:///{curDir}/WebPages/index.html", UriKind.RelativeOrAbsolute));
        }
    }
}
