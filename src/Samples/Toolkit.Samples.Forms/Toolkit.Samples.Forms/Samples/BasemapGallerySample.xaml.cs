using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Toolkit.Samples.Forms.Samples
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BasemapGallerySample : ContentPage
    {
        public BasemapGallerySample()
        {
            InitializeComponent();
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);
        }
    }
}