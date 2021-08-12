using System;
using Windows.UI.Xaml.Controls;
using Esri.ArcGISRuntime.UI.Controls;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Collections.Generic;
using System.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Esri.ArcGISRuntime.Toolkit.SampleApp.Samples.UtilityTraceResultView
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UtilityTraceResultViewSample : Page
    {

        private const string PortalItem = "https://sampleserver7.arcgisonline.com/portal/home/item.html?id=2f4e014dce8b4b7797146e198893c5f3";
        private const string PortalUrl = "https://sampleserver7.arcgisonline.com/portal/sharing/rest";
        private const string PortalUsername = "viewer01";
        private const string PortalPassword = "I68VGU^nMurF";

        public UtilityTraceResultViewSample()
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

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(message))
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
                if (MyMapView.Map?.UtilityNetworks?.FirstOrDefault() is UtilityNetwork utilityNetwork)
                {
                    if (utilityNetwork.LoadStatus != LoadStatus.Loaded)
                    {
                        await utilityNetwork.LoadAsync();
                    }

                    var layerResults = await MyMapView.IdentifyLayersAsync(e.Position, 10, false);

                    if (GetTraceLocation(utilityNetwork, layerResults) is UtilityElement startingLocation)
                    {
                        if (startingLocation.AssetType?.TerminalConfiguration?.Terminals?.Count > 1)
                        {
                            startingLocation.Terminal = startingLocation.AssetType.TerminalConfiguration.Terminals[0];
                        }

                        var parameters = new UtilityTraceParameters(UtilityTraceType.Downstream, new[] { startingLocation });
                        parameters.ResultTypes.Add(UtilityTraceResultType.FunctionOutputs);
                        var domainNetwork = utilityNetwork.Definition.GetDomainNetwork("ElectricDistribution");
                        var tier = domainNetwork?.GetTier("Medium Voltage Radial");
                        parameters.TraceConfiguration = tier?.TraceConfiguration;

                        var results = await utilityNetwork.TraceAsync(parameters);
                        foreach (var result in results)
                        {
                            if (result is UtilityElementTraceResult elementTraceResult)
                            {
                                UtilityTraceResultViewForElements.TraceResult = elementTraceResult;
                            }
                            else if (result is UtilityFunctionTraceResult functionTraceResult)
                            {
                                UtilityTraceResultViewForFunctions.TraceResult = functionTraceResult;
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

        private UtilityElement GetTraceLocation(UtilityNetwork utilityNetwork, IEnumerable<IdentifyLayerResult> layerResults)
        {

            foreach (var layerResult in layerResults)
            {

                foreach (var geoElement in layerResult.GeoElements)
                {
                    if (geoElement is ArcGISFeature feature)
                    {
                        return utilityNetwork.CreateElement(feature);
                    }
                }


                foreach (var sublayerResult in layerResult.SublayerResults)
                {
                    foreach (var geoElement in sublayerResult.GeoElements)
                    {
                        if (geoElement is ArcGISFeature feature)
                        {
                            return utilityNetwork.CreateElement(feature);
                        }
                    }
                }
            }

            return null;
        }
    }
}