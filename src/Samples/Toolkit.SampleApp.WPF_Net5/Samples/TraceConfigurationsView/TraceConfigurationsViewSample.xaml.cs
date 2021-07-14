using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Esri.ArcGISRuntime.Toolkit.Samples.TraceConfigurationsView
{
    /// <summary>
    /// Interaction logic for TraceConfigurationsViewSample.xaml
    /// </summary>
    [SampleInfoAttribute(Category = "TraceConfigurationsView", DisplayName = "TraceConfigurationsView - Comprehensive", Description = "Full TraceConfigurationsView scenario")]
    public partial class TraceConfigurationsViewSample : UserControl
    {
        private const string Portal1Item1 = "https://<hostname1>/portal/home/item.html?id=<itemId1>";
        private const string Portal1Item2 = "https://<hostname1>/portal/home/item.html?id=<itemId2>";
        private const string Portal2FeatureServer = "";

        private readonly Tuple<string, string, string> _portal1Login = new Tuple<string, string, string>("https://<hostname1>/portal/sharing/rest", "<username>", "<password>");
        private readonly Tuple<string, string, string> _portal2Login = new Tuple<string, string, string>("http://<hostname2>/portal/sharing/rest", "<username>", "<password>");

        public TraceConfigurationsViewSample()
        {
            InitializeComponent();
            Initialize();
            SubscribeToEvents();
        }

        private async void Initialize()
        {
            try
            {

                var portal1Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(_portal1Login.Item1), _portal1Login.Item2, _portal1Login.Item3);
                AuthenticationManager.Current.AddCredential(portal1Credential);

                var portal2Credential = await AuthenticationManager.Current.GenerateCredentialAsync(new Uri(_portal2Login.Item1), _portal2Login.Item2, _portal2Login.Item3);
                AuthenticationManager.Current.AddCredential(portal2Credential);

                MyMapView.Map = new Map(new Uri(Portal1Item1));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initializing sample failed: {ex.Message}", ex.GetType().Name);
            }
        }

        private bool _isSubscribedToEvents = false;
        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                TraceConfigurationsView.UtilityNetworkChanged += OnUtilityNetworkChanged;
                TraceConfigurationsView.TraceConfigurationsChanged += OnTraceConfigurationsChanged;
                TraceConfigurationsView.TraceLocationTapped += OnTraceLocationTapped;
                TraceConfigurationsView.TraceCompleted += OnTraceCompleted;

            }
            else
            {
                TraceConfigurationsView.UtilityNetworkChanged -= OnUtilityNetworkChanged;
                TraceConfigurationsView.TraceConfigurationsChanged -= OnTraceConfigurationsChanged;
                TraceConfigurationsView.TraceLocationTapped -= OnTraceLocationTapped;
                TraceConfigurationsView.TraceCompleted -= OnTraceCompleted;
            }
            _isSubscribedToEvents = !_isSubscribedToEvents;
        }

        private void OnSetGeoView(object sender, RoutedEventArgs e)
        {
            MyMapView.Visibility = MyMapView.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            MySceneView.Visibility = MySceneView.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            Binding geoviewBinding = new Binding
            {
                Source = MyMapView.Visibility == Visibility.Visible ? (GeoView)MyMapView : MySceneView
            };
            TraceConfigurationsView.SetBinding(UI.Controls.TraceConfigurationsView.GeoViewProperty, geoviewBinding);
        }
        
        private void OnSetGeoModel(object sender, RoutedEventArgs e)
        {
            if (TraceConfigurationsView.GeoView is MapView mapView)
            {
                if(mapView.Map.Uri?.OriginalString != Portal1Item1)
                {

                    mapView.Map = new Map(new Uri(Portal1Item1));
                }
                else
                {
                    mapView.Map = new Map(new Uri(Portal1Item2));
                }
            }
            else if (TraceConfigurationsView.GeoView is SceneView sceneView)
            {
                if (Basemap.CreateStreetsNightVector() is Basemap basemap)
                {
                    sceneView.Scene = sceneView.Scene.Basemap == basemap ? new Scene(Basemap.CreateTopographicVector()) : 
                        new Scene(basemap);
                }
            }
        }

        private async void OnAddRemoveUtilityNetwork(object sender, RoutedEventArgs e)
        {
            if (TraceConfigurationsView.GeoView is MapView mapView && mapView.Map is Map map)
            {
                if (map.UtilityNetworks.Count == 1)
                {
                    var serviceGeodatabase = new ServiceGeodatabase(new Uri(Portal2FeatureServer));
                    await serviceGeodatabase.LoadAsync();
                    map.OperationalLayers.Add(new FeatureLayer(serviceGeodatabase.GetTable(3)));
                    map.OperationalLayers.Add(new FeatureLayer(serviceGeodatabase.GetTable(0)));
                    map.UtilityNetworks.Add(new UtilityNetwork(new Uri(Portal2FeatureServer)));
                }
                else
                {
                    map.UtilityNetworks.RemoveAt(map.UtilityNetworks.Count - 1);
                }
            }
        }

        private void OnUpdateSymbols(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            var colorBytes = new byte[3];
            random.NextBytes(colorBytes);
            var color = Color.FromArgb(200, colorBytes[0], colorBytes[1], colorBytes[2]);
            TraceConfigurationsView.StartingLocationSymbol = new SimpleMarkerSymbol
            {
                Color = color,
                Style = (SimpleMarkerSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).Length - 1),
                Size = 20d,
                Outline = null,
            };
            random.NextBytes(colorBytes);
            color = Color.FromArgb(200, colorBytes[0], colorBytes[1], colorBytes[2]);
            TraceConfigurationsView.ResultPointSymbol = new SimpleMarkerSymbol
            {
                Color = color,
                Style = (SimpleMarkerSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).Length - 1),
                Size = 20d,
                Outline = null,
            };
            TraceConfigurationsView.ResultLineSymbol = new SimpleLineSymbol
            {
                Color = color,
                Style = (SimpleLineSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleLineSymbolStyle)).Length - 1),
                Width = 5d,
            };
            TraceConfigurationsView.ResultFillSymbol = new SimpleFillSymbol
            {
                Color = color,
                Style = (SimpleFillSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleFillSymbolStyle)).Length - 1),
                Outline = null,
            };
        }

        private void OnAddRemoveEventSubscription(object sender, RoutedEventArgs e)
        {
            SubscribeToEvents();
        }

        private void OnUtilityNetworkChanged(object sender, UI.Controls.UtilityNetworkChangedEventArgs e)
        {
            if (e.UtilityNetwork != null)
            {
                MessageBox.Show($"'{e.UtilityNetwork.Name}' is selected.", "UtilityNetworkChanged");
            }
            else
            {
                MessageBox.Show("UtilityNetwork was cleared.", "UtilityNetworkChanged");
            }
        }

        private void OnTraceConfigurationsChanged(object sender, UI.Controls.TraceConfigurationsChangedEventArgs e)
        {
            MessageBox.Show($"'{e.TraceConfigurations.Count()}' trace configuration(s) found.", "TraceConfigurationsChanged");
        }

        private void OnTraceLocationTapped(object sender, UI.Controls.TraceLocationTappedEventArgs e)
        {
            MessageBox.Show($"'{e.TraceLocation.AssetGroup.Name}' trace location is tapped.", "TraceLocationTappedEventArgs");
        }

        private void OnTraceCompleted(object sender, UI.Controls.TraceCompletedEventArgs e)
        {
            if (e.Error is Exception error)
            {
                MessageBox.Show(error.Message, error.GetType().Name);
            }
            else
            {
                MessageBox.Show($"'{e.Results.Count()}' result(s) returned.", "TraceCompleted");
            }
        }

        private void OnSetItemTemplate(object sender, RoutedEventArgs e)
        {
            if (Resources["ItemTemplateOne"] is DataTemplate templateOne && Resources["ItemTemplateTwo"] is DataTemplate templateTwo)
            {
                TraceConfigurationsView.ItemTemplate = TraceConfigurationsView.ItemTemplate == templateOne ? templateTwo : templateOne;
            }
        }

        private void OnSetItemContainerStyle(object sender, RoutedEventArgs e)
        {
            if (TraceConfigurationsView.ItemContainerStyle == null && Resources["AlternateItemContainerStyle"] is Style newStyle)
            {
                TraceConfigurationsView.ItemContainerStyle = newStyle;
            }
            else
            {
                TraceConfigurationsView.ItemContainerStyle = null;
            }
        }

        private void OnSetTerminalPickerTemplate(object sender, RoutedEventArgs e)
        {
            if (this.Resources["TerminalPickerTemplate"] is FrameworkElement frameworkElement)
            {
                TraceConfigurationsView.TerminalPickerTemplate = TraceConfigurationsView.TerminalPickerTemplate == null ? frameworkElement : null;
            }
        }

        private void OnSetResultItemTemplate(object sender, RoutedEventArgs e)
        {
            if (this.Resources["ResultItemTemplate"] is DataTemplate resultItemTemplate)
            {
                TraceConfigurationsView.ResultItemTemplate = TraceConfigurationsView.ResultItemTemplate == null ? resultItemTemplate : null;
            }
        }

        private void OnSetResultItemContainerStyle(object sender, RoutedEventArgs e)
        {
            if (this.Resources["ItemsControlStyle"] is Style newStyle)
            {
                TraceConfigurationsView.ResultItemContainerStyle = TraceConfigurationsView.ResultItemContainerStyle == null ? newStyle : null;
            }
        }
    }
}