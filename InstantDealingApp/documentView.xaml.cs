using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace InstantDealingApp
{
    /// <summary>
    /// Interaction logic for documentView.xaml
    /// </summary>
    public partial class documentView : Window
    {
        public documentView( FlowDocument doc)
        {


            InitializeComponent();
            docViewer.Document = doc;
        }

        private void Window_LostFocus(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
