using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Esri.ArcGISRuntime.Toolkit.Samples.UtilityTraceResultView
{
    /// <summary>
    /// Interaction logic for UtilityTraceResultViewSample.xaml
    /// </summary>
    public partial class UtilityTraceResultViewSample : UserControl
    {
        private const string PortalItem = "https://sampleserver7.arcgisonline.com/portal/home/item.html?id=2f4e014dce8b4b7797146e198893c5f3";
        private const string PortalUrl = "https://sampleserver7.arcgisonline.com/portal/sharing/rest";
        private const string PortalUsername = "viewer01";
        private const string PortalPassword = "I68VGU^nMurF";

        public UtilityTraceResultViewSample()
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
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButton.OK);
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