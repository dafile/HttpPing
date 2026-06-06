using System.Windows;

namespace HttpPing
{
    public partial class AddHostDialog : Window
    {
        public string EnteredUrl { get; private set; } = "";
        public string EnteredAlias { get; private set; } = "";

        public AddHostDialog()
        {
            InitializeComponent();
            UrlBox.Focus();
            UrlBox.SelectAll();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlBox.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请输入网址。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            EnteredUrl = url;
            EnteredAlias = AliasBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
