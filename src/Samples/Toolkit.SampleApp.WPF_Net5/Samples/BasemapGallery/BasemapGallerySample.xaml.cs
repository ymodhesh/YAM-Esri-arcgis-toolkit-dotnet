using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
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

namespace Esri.ArcGISRuntime.Toolkit.Samples.BasemapGallery
{
    /// <summary>
    /// Interaction logic for BasemapGallerySample.xaml
    /// </summary>
    [SampleInfoAttribute(Category = "BasemapGallery", DisplayName = "BasemapGallery", Description = "Full BasemapGallery scenario")]
    public partial class BasemapGallerySample : UserControl
    {
        public BasemapGallerySample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }
    }
}
