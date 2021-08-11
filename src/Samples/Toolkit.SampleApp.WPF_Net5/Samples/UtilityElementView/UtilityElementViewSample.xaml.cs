using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.UtilityElementView
{
    /// <summary>
    /// Interaction logic for UtilityElementViewSample.xaml
    /// </summary>
    public partial class UtilityElementViewSample : UserControl
    {
        private const string PortalItem = "https://sampleserver7.arcgisonline.com/portal/home/item.html?id=2f4e014dce8b4b7797146e198893c5f3";
        private const string PortalUrl = "https://sampleserver7.arcgisonline.com/portal/sharing/rest";
        private const string PortalUsername = "viewer01";
        private const string PortalPassword = "I68VGU^nMurF";

        public UtilityElementViewSample()
        {
            InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(PortalUrl), PortalUsername, PortalPassword);
                AuthenticationManager.Current.AddCredential(credential);

                MyMapView.Map = new Map(new Uri(PortalItem));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK);
            }
        }

        private async void OnGeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                var layerResults = await MyMapView.IdentifyLayersAsync(e.Position, 10, false);

                foreach (var layerResult in layerResults)
                {

                    foreach (var geoElement in layerResult.GeoElements)
                    {
                        if (geoElement is ArcGISFeature feature)
                        {
                            UtilityElementView.Feature = feature;
                            return;
                        }
                    }


                    foreach (var sublayerResult in layerResult.SublayerResults)
                    {
                        foreach (var geoElement in sublayerResult.GeoElements)
                        {
                            if (geoElement is ArcGISFeature feature)
                            {
                                UtilityElementView.Feature = feature;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK);
            }
        }
    }
}