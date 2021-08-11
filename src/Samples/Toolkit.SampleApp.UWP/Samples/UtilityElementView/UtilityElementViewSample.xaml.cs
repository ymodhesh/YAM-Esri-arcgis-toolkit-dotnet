using System;
using Windows.UI.Xaml.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Data;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.UtilityElementView
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UtilityElementViewSample : Page
    {
       
        private const string PortalItem = "https://sampleserver7.arcgisonline.com/portal/home/item.html?id=2f4e014dce8b4b7797146e198893c5f3";
        private const string PortalUrl = "https://sampleserver7.arcgisonline.com/portal/sharing/rest";
        private const string PortalUsername = "viewer01";
        private const string PortalPassword = "I68VGU^nMurF";

        public UtilityElementViewSample()
        {
            this.InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            string title = null;
            string message = null;
            try
            {
                var credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(PortalUrl), PortalUsername, PortalPassword);
                AuthenticationManager.Current.AddCredential(credential);

                MyMapView.Map = new Map(new Uri(PortalItem));
            }
            catch (Exception ex)
            {
                title = ex.GetType().Name;
                message = ex.Message;
            }

            if(!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message))
            {
                await new MessageDialog(message, title).ShowAsync();
            }
        }

        private async void OnGeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            string title = null;
            string message = null;
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
                title = ex.GetType().Name;
                message = ex.Message;
            }

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message))
            {
                await new MessageDialog(message, title).ShowAsync();
            }
        }
    }
}
