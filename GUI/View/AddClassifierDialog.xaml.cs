using System;
using System.Windows;
using Recliner2GCBM.Configuration;
using Recliner2GCBM.ViewModel;

namespace Recliner2GCBM.View
{
    /// <summary>
    /// Interaction logic for AddClassifier.xaml
    /// </summary>
    public partial class AddClassifierDialog : Window
    {
        public AddClassifierDialog(ClassifierConfiguration classifier)
        {
            InitializeComponent();
            DataContext = new AddClassifierDialogViewModel(classifier, inputDataView);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            ((AddClassifierDialogViewModel)DataContext).Dispose();
            base.OnClosed(e);
        }
    }
}
