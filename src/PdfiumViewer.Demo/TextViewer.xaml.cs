using System.Windows;

namespace PdfiumViewer.Demo
{
    /// <summary>
    /// Interaction logic for TextViewer.xaml
    /// </summary>
    public partial class TextViewer : Window
    {
        public TextViewer()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string Body { get; set; }
        public string Caption { get; set; }
    }
}
