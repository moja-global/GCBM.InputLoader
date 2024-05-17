using System.Windows.Controls;
using Recliner2GCBM.ViewModel;

namespace Recliner2GCBM.View
{
    /// <summary>
    /// Interaction logic for GrowthCurveTab.xaml
    /// </summary>
    public partial class GrowthCurveTab : UserControl
    {
        public GrowthCurveTab()
        {
            InitializeComponent();
            ((GrowthCurveTabViewModel)DataContext).InputDataView = grid;
        }
    }
}
