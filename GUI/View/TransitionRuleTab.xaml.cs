using System.Windows.Controls;
using Recliner2GCBM.ViewModel;

namespace Recliner2GCBM.View
{
    /// <summary>
    /// Interaction logic for TransitionRuleTab.xaml
    /// </summary>
    public partial class TransitionRuleTab : UserControl
    {
        public TransitionRuleTab()
        {
            InitializeComponent();
            ((TransitionRuleTabViewModel)DataContext).InputDataView = grid;
        }
    }
}
