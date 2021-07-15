// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// Represents a control that enables a user to select a <see cref="UtilityNamedTraceConfiguration"/> to perform a trace.
    /// </summary>
    public class TraceConfigurationsView : TemplatedView
    {
        private ObservableCollection<UtilityNetwork> _utilityNetworks = new ObservableCollection<UtilityNetwork>();
        private ObservableCollection<UtilityNamedTraceConfiguration> _traceConfigurations = new ObservableCollection<UtilityNamedTraceConfiguration>();
        private readonly ObservableCollection<UtilityTraceFunctionOutput> _traceFunctionResults = new ObservableCollection<UtilityTraceFunctionOutput>();

        private readonly List<UtilityElement> _startingLocations = new List<UtilityElement>();

        private readonly Symbol _defaultStartingLocationSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.LimeGreen, 20d);
        private readonly Symbol _defaultResultPointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Blue, 20d);
        private readonly Symbol _defaultResultLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, Color.Blue, 5d);
        private readonly Symbol _defaultResultFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.ForwardDiagonal, Color.Blue, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 2d));

        private readonly GraphicsOverlay _traceLocationsGraphicsOverlay = new GraphicsOverlay();
        private readonly GraphicsOverlay _resultGraphicsOverlay = new GraphicsOverlay();

        private SynchronizationContext _synchronizationContext;

        private ListView _utilityNetworkPicker;
        private ListView _traceConfigurationPicker;
        private ProgressBar _busyIndicator;
        private Label _statusLabel;
        private ListView _functionResultList;

        private static readonly DataTemplate DefaultDataTemplate;
        private static readonly ControlTemplate DefaultControlTemplate;

        static TraceConfigurationsView()
        {
            // DefaultDataTemplate = new DataTemplate(() => { // TODO });
            string template =
                @"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
                    <Grid>
                            <Grid.Resources>
                                <ContentView x:Key=""TerminalPickerTemplate"">
                                    <Grid>
                                        <Grid.RowDefinitions>
                                            <RowDefinition Height=""Auto"" />
                                            <RowDefinition Height=""Auto"" />
                                            <RowDefinition Height=""Auto"" />
                                        </Grid.RowDefinitions>
                                        <Label Text=""Select a terminal""
                                               HorizontalOptions=""Fill""
                                               FontAttributes=""Bold""
                                                   Margin=""2"" />
                                        <ListView ItemsSource=""{Binding Element.AssetType.TerminalConfiguration.Terminals}""
                                                  SelectedItem=""{Binding Element.Terminal, Mode=TwoWay}""
                                                  HorizontalOptions=""Fill""
                                                  Margin=""2""
                                                  Grid.Row=""1"">
                                            <ListView.ItemTemplate>
                                                <DataTemplate>
                                                    <Grid>
                                                        <Grid.RowDefinitions>
                                                            <RowDefinition Height=""Auto"" />
                                                            <RowDefinition Height=""Auto"" />
                                                        </Grid.RowDefinitions>
                                                        <Label Text=""{Binding Name}""
                                                               FontAttributes=""Bold""
                                                               HorizontalOptions=""Fill""
                                                               Margin=""2"" />
                                                        <StackLayout Orientation=""Horizontal""
                                                                    HorizontalOptions=""Fill""
                                                                    Margin=""2""
                                                                    Grid.Row=""1"">
                                                            <Label Text=""Is Upstream: "" />
                                                            <Label Text=""{Binding IsUpstreamTerminal}"" />
                                                        </StackLayout>
                                                    </Grid>
                                                </DataTemplate>
                                            </ListView.ItemTemplate>
                                        </ListView>
                                        <Button Text=""Select""
                                                Command=""{Binding SelectCommand}""
                                                CommandParameter=""{Binding Element}""
                                                HorizontalOptions=""Fill""
                                                Margin=""2""
                                                Grid.Row=""2"" />
                                    </Grid>
                                </ContentView>
                                <DataTemplate x:Key=""ItemTemplate"">
                                    <Grid>
                                        <Grid.RowDefinitions>
                                            <RowDefinition Height=""Auto"" />
                                            <RowDefinition Height=""Auto"" />
                                        </Grid.RowDefinitions>
                                        <Label Text=""{Binding Name}""
                                               FontAttributes=""Bold""
                                               HorizontalOptions=""Fill""
                                               Margin=""2"" />
                                        <StackLayout Orientation=""Horizontal""
                                                    HorizontalOptions=""Fill""
                                                    Margin=""2""
                                                    Grid.Row=""1"">
                                            <Label Text=""Trace Type: "" />
                                            <Label Text=""{Binding TraceType}"" />
                                        </StackLayout>
                                    </Grid>
                                </DataTemplate>
                                <DataTemplate x:Key=""ResultItemTemplate"">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition />
                                            <ColumnDefinition />
                                        </Grid.ColumnDefinitions>
                                        <Grid>
                                            <Grid.RowDefinitions>
                                                <RowDefinition Height=""Auto"" />
                                                <RowDefinition Height=""Auto"" />
                                            </Grid.RowDefinitions>
                                            <Label Text=""{Binding Function.FunctionType}""
                                                       FontAttributes=""Bold""
                                                       HorizontalOptions=""Fill""
                                                       Margin=""2"" />
                                            <Label Text=""{Binding Function.NetworkAttribute.Name}""
                                                   FontAttributes=""Bold""
                                                       HorizontalOptions=""Fill""
                                                       Margin=""2""
                                                       Grid.Row=""1"" />
                                        </Grid>
                                        <Label Text=""{Binding Result}""
                                                   Margin=""2""
                                                   VerticalOptions=""Center""
                                                   HorizontalOptions=""Fill""
                                                   Grid.Column=""1"" />
                                    </Grid>
                                </DataTemplate>
                            </Grid.Resources>
                            <Grid.RowDefinitions>
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                                <RowDefinition Height=""Auto"" />
                            </Grid.RowDefinitions>
                            <Label Text=""Select a utility network""
                                   IsVisible=""{Binding Source={Reference UtilityNetworkPicker}, Path=IsVisible}""
                                   FontAttributes=""Bold""
                                   Margin=""2"" />
                                <ListView x:Name=""UtilityNetworkPicker""
                                        ItemTemplate=""{StaticResource ItemTemplate}""
                                        IsVisible=""True""
                                    Margin=""2""
                                    Grid.Row=""1"" />
                            <Label Text=""Select a trace configuration""
                                   IsVisible=""{Binding Source={Reference TraceConfigurationPicker}, Path=IsVisible}""
                                   FontAttributes=""Bold""
                                   Margin=""2""
                                   Grid.Row=""2"" />
                            <ListView x:Name=""TraceConfigurationPicker""
                                      ItemTemplate=""{StaticResource ItemTemplate}""
                                    IsVisible=""True""
                                    Margin=""2""
                                    Grid.Row=""3"" />
                            <StackLayout Orientation=""Horizontal""
                                         Margin=""2""
                                         Grid.Row=""4"">
                                <Switch x:Name=""AddingLocationToggle"" />
                                <Label Text=""Add Starting Location"" />
                            </StackLayout>
                            <Grid Grid.Row=""5"">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition />
                                    <ColumnDefinition />
                                </Grid.ColumnDefinitions>
                                <Button x:Name=""TraceButton""
                                        Text=""Trace""
                                        Margin=""2"" />
                                <Button x:Name=""ResetButton""
                                        Text=""Reset""
                                        Margin=""2""
                                        Grid.Column=""1"" />
                            </Grid>
                            <ProgressBar x:Name=""BusyIndicator""
                                         IsVisible=""True""
                                         Margin=""2""
                                         Grid.Row=""6"" />
                            <Label x:Name=""StatusLabel""
                                   Text=""{Binding Status}""
                                   Margin=""2""
                                   Grid.Row=""7"" />
                            <Label Text=""Trace function results""
                                   IsVisible=""{Binding Source={Reference FunctionResultList}, Path=IsVisible}""
                                   FontAttributes=""Bold""
                                   Margin=""2""
                                   Grid.Row=""8"" />
                            <ListView x:Name=""FunctionResultList""
                                      ItemTemplate=""{StaticResource ResultItemTemplate}""
                                      IsVisible=""False""
                                      Margin=""2""
                                      Grid.Row=""9"" />
                            </Grid>
                    </ControlTemplate>";

            // DefaultControlTemplate = Extensions.LoadFromXaml(new ControlTemplate(), template);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceConfigurationsView"/> class.
        /// </summary>
        public TraceConfigurationsView()
        {
            ItemTemplate = DefaultDataTemplate;

            ControlTemplate = DefaultControlTemplate;
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("UtilityNetworkPicker") is ListView utilityNetworkPicker)
            {
                _utilityNetworkPicker = utilityNetworkPicker;
                _utilityNetworkPicker.ItemsSource = _utilityNetworks;
                _utilityNetworks.CollectionChanged += (s, e) =>
                {
                    if (_utilityNetworkPicker != null)
                    {
                        _utilityNetworkPicker.IsVisible = _utilityNetworks.Count > 1;
                    }
                };
                _utilityNetworkPicker.ItemSelected += (s, e) => SelectedUtilityNetwork = e.SelectedItem as UtilityNetwork;
            }

            if (GetTemplateChild("TraceConfigurationPicker") is ListView traceConfigurationPicker)
            {
                _traceConfigurationPicker = traceConfigurationPicker;
                _traceConfigurationPicker.ItemsSource = _traceConfigurations;
                _traceConfigurations.CollectionChanged += (s, e) =>
                {
                    if (_traceConfigurationPicker != null)
                    {
                        _traceConfigurationPicker.IsVisible = _traceConfigurations.Count > 1;
                    }
                };
                _utilityNetworkPicker.ItemSelected += (s, e) => SelectedTraceConfiguration = e.SelectedItem as UtilityNamedTraceConfiguration;
            }

            if (GetTemplateChild("AddingLocationToggle") is Switch addingLocationToggle)
            {
                addingLocationToggle.Toggled += (s, e) => IsAddingTraceLocation = e.Value;
            }

            if (GetTemplateChild("ResetButton") is Button resetButton)
            {
                resetButton.Clicked += (s, e) => Reset();
            }

            if (GetTemplateChild("TraceButton") is Button traceButton)
            {
                traceButton.Clicked += (s, e) => _ = TraceAsync();
            }

            if (GetTemplateChild("BusyIndicator") is ProgressBar busyIndicator)
            {
                _busyIndicator = busyIndicator;
            }

            if (GetTemplateChild("StatusLabel") is Label statusLabel)
            {
                _statusLabel = statusLabel;
            }

            if (GetTemplateChild("FunctionResultList") is ListView functionResultList)
            {
                _functionResultList = functionResultList;
                _functionResultList.ItemsSource = _traceFunctionResults;
                _traceFunctionResults.CollectionChanged += (s, e) =>
                {
                    if (_functionResultList != null)
                    {
                        _functionResultList.IsVisible = _traceFunctionResults.Count > 0;
                    }
                };
            }

            _propertyChangedAction = new Action<string>((propertyName) =>
            {
                if (propertyName == nameof(IsBusy))
                {
                    if (_busyIndicator != null)
                    {
                        _busyIndicator.IsVisible = IsBusy;
                    }

                    IsEnabled = !IsBusy;
                }
                else if (propertyName == nameof(Status))
                {
                    if (_statusLabel != null)
                    {
                        _statusLabel.Text = Status;
                    }
                }
            });

            Status = GetStatusBasedOnSelection();
        }

        /// <summary>
        /// Gets or sets the <see cref="GeoView"/> that diplays the <see cref="Map"/> or <see cref="Scene"/>, which contains
        /// one or more <see cref="UtilityNetwork"/>s for which <see cref="UtilityNamedTraceConfiguration"/>s are defined.
        /// </summary>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView" /> dependency property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null, propertyChanged: OnGeoViewPropertyChanged);

        /// <summary>
        /// Handles property changes for the <see cref="GeoView" /> bindable property.
        /// </summary>
        private static void OnGeoViewPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            ((TraceConfigurationsView)sender).UpdateGeoView(oldValue as GeoView, newValue as GeoView);
        }

        private bool AutoZoomToTraceResults
        {
            get { return (bool)GetValue(AutoZoomProperty); }
            set { SetValue(AutoZoomProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoZoomToTraceResults" /> dependency property.
        /// </summary>
        public static readonly BindableProperty AutoZoomProperty =
            BindableProperty.Create(nameof(AutoZoomToTraceResults), typeof(bool), typeof(TraceConfigurationsView), true, BindingMode.OneWay, null);

        private Symbol StartingLocationSymbol
        {
            get { return (Symbol)GetValue(StartingLocationSymbolProperty); }
            set { SetValue(StartingLocationSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationSymbol" /> dependency property.
        /// </summary>
        public static readonly BindableProperty StartingLocationSymbolProperty =
            BindableProperty.Create(nameof(StartingLocationSymbol), typeof(Symbol), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null, propertyChanged: OnStartingLocationSymbolPropertyChanged);

        private static void OnStartingLocationSymbolPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            ((TraceConfigurationsView)sender).UpdateTraceLocationSymbol(newValue as Symbol);
        }

        private Symbol ResultPointSymbol
        {
            get { return (Symbol)GetValue(ResultPointSymbolProperty); }
            set { SetValue(ResultPointSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultPointSymbol" /> dependency property.
        /// </summary>
        public static readonly BindableProperty ResultPointSymbolProperty =
            BindableProperty.Create(nameof(ResultPointSymbol), typeof(Symbol), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null, propertyChanged: OnResultPointSymbolPropertyChanged);

        private static void OnResultPointSymbolPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            ((TraceConfigurationsView)sender).UpdateResultSymbol(newValue as Symbol, GeometryType.Multipoint);
        }

        private Symbol ResultLineSymbol
        {
            get { return (Symbol)GetValue(ResultLineSymbolProperty); }
            set { SetValue(ResultLineSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultLineSymbol" /> dependency property.
        /// </summary>
        public static readonly BindableProperty ResultLineSymbolProperty =
            BindableProperty.Create(nameof(ResultLineSymbol), typeof(Symbol), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null, propertyChanged: OnResultLineSymbolPropertyChanged);

        private static void OnResultLineSymbolPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            ((TraceConfigurationsView)sender).UpdateResultSymbol(newValue as Symbol, GeometryType.Polyline);
        }

        private Symbol ResultFillSymbol
        {
            get { return (Symbol)GetValue(ResultFillSymbolProperty); }
            set { SetValue(ResultFillSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultFillSymbol" /> dependency property.
        /// </summary>
        public static readonly BindableProperty ResultFillSymbolProperty =
            BindableProperty.Create(nameof(ResultFillSymbol), typeof(Symbol), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null, propertyChanged: OnResultFillSymbolPropertyChanged);

        private static void OnResultFillSymbolPropertyChanged(BindableObject sender, object oldValue, object newValue)
        {
            ((TraceConfigurationsView)sender).UpdateResultSymbol(newValue as Symbol, GeometryType.Polygon);
        }

        private Task<UtilityElement> GetElementWithTerminalAsync(MapPoint location, UtilityElement element)
        {
            var tcs = new TaskCompletionSource<UtilityElement>();

            // TODO
            return tcs.Task;
        }

        /// <summary>
        /// Gets or sets the item template used to render trace configuration entries in the list.
        /// </summary>
        public VisualElement TerminalPickerTemplate
        {
            get { return (VisualElement)GetValue(TerminalPickerTemplateProperty); }
            set { SetValue(TerminalPickerTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TerminalPickerTemplate"/> dependency property.
        /// </summary>
        public static readonly BindableProperty TerminalPickerTemplateProperty =
            BindableProperty.Create(nameof(TerminalPickerTemplate), typeof(VisualElement), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null);

        /// <summary>
        /// Gets or sets the item template used to render trace configuration entries in the list.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null);

        /// <summary>
        /// Gets or sets the style used by the list view items in the underlying list view control.
        /// </summary>
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ItemContainerStyleProperty =
            BindableProperty.Create(nameof(ItemContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null);

        /// <summary>
        /// Gets or sets the item template used to render trace configuration entries in the list.
        /// </summary>
        public DataTemplate ResultItemTemplate
        {
            get { return (DataTemplate)GetValue(ResultItemTemplateProperty); }
            set { SetValue(ResultItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemTemplate"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ResultItemTemplateProperty =
            BindableProperty.Create(nameof(ResultItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null);

        /// <summary>
        /// Gets or sets the style used by the list view items in the underlying list view control.
        /// </summary>
        public Style ResultItemContainerStyle
        {
            get { return (Style)GetValue(ResultItemContainerStyleProperty); }
            set { SetValue(ResultItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ResultItemContainerStyleProperty =
            BindableProperty.Create(nameof(ResultItemContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null);

        /// <summary>
        /// Occurs when a <see cref="UtilityNetwork"/> is selected.
        /// </summary>
        public event EventHandler<UtilityNetworkChangedEventArgs> UtilityNetworkChanged;

        /// <summary>
        /// Occurs when the collection of <see cref="UtilityNamedTraceConfiguration"/> has changed.
        /// </summary>
        public event EventHandler<TraceConfigurationsChangedEventArgs> TraceConfigurationsChanged;

        /// <summary>
        /// Occurs when a <see cref="UtilityElement"/> is tapped.
        /// </summary>
        public event EventHandler<TraceLocationTappedEventArgs> TraceLocationTapped;

        /// <summary>
        /// Occurs when <see cref="UtilityNetwork.TraceAsync(UtilityTraceParameters)"/> has completed.
        /// </summary>
        public event EventHandler<TraceCompletedEventArgs> TraceCompleted;

        private UtilityNetwork _selectedUtilityNetwork;

        private UtilityNetwork SelectedUtilityNetwork
        {
            get => _selectedUtilityNetwork;
            set
            {
                if (_selectedUtilityNetwork != value)
                {
                    _selectedUtilityNetwork = value;

                    Status = GetStatusBasedOnSelection();

                    if (UtilityNetworkChanged != null)
                    {
                        UtilityNetworkChanged.Invoke(this, new UtilityNetworkChangedEventArgs(_selectedUtilityNetwork));
                    }

                    _ = GetTraceConfigurationsAsync();
                }
            }
        }

        private UtilityTraceParameters _traceParameters;

        private UtilityTraceParameters TraceParameters
        {
            get => _traceParameters;
            set
            {
                if (_traceParameters != value)
                {
                    _traceParameters = value;
                }
            }
        }

        private UtilityNamedTraceConfiguration _selectedTraceConfiguration;

        private UtilityNamedTraceConfiguration SelectedTraceConfiguration
        {
            get => _selectedTraceConfiguration;
            set
            {
                if (_selectedTraceConfiguration != value)
                {
                    _selectedTraceConfiguration = value;

                    if (_selectedTraceConfiguration is UtilityNamedTraceConfiguration namedTraceConfiguration)
                    {
                        TraceParameters = new UtilityTraceParameters(namedTraceConfiguration, _startingLocations);
                    }

                    Status = GetStatusBasedOnSelection();
                }
            }
        }

        private string GetStatusBasedOnSelection()
        {
            var stringBuilder = new StringBuilder();

            if (_utilityNetworks.Count == 0)
            {
                stringBuilder.AppendLine("Loading utility networks...");
            }
            else if (SelectedUtilityNetwork == null && _utilityNetworks.Count > 1)
            {
                stringBuilder.AppendLine("Select a utility network.");
            }
            else if (_traceConfigurations.Count == 0)
            {
                stringBuilder.AppendLine("Loading trace configurations...");
            }
            else if (SelectedTraceConfiguration == null && _traceConfigurations.Count > 1)
            {
                stringBuilder.AppendLine("Select a trace configuration.");
            }
            else
            {
                var minimum = SelectedTraceConfiguration == null ? 0 : (SelectedTraceConfiguration.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ? 2 : 1);

                if (TraceParameters != null)
                {
                    stringBuilder.AppendLine($"This '{TraceParameters.TraceType}' trace requires at least '{minimum}' starting location(s).");
                }

                if (IsAddingTraceLocation)
                {
                    stringBuilder.AppendLine("Tap a feature to identify a starting location.");
                }
                else
                {
                    stringBuilder.AppendLine("Toggle on 'Add Starting Location' button.");
                }

                if (minimum > 0 && _startingLocations.Count >= minimum)
                {
                    stringBuilder.AppendLine("Or click 'Trace' button.");
                }
            }

            return stringBuilder.ToString();
        }

        private string _status;

        private string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPrivatePropertyChanged();
                }
            }
        }

        private bool _isBusy;

        private bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPrivatePropertyChanged();
                }
            }
        }

        private Action<string> _propertyChangedAction;

        private void OnPrivatePropertyChanged([CallerMemberName] string propertyName = null)
        {
            RunOnUIThread(() =>
            {
                if (_propertyChangedAction != null)
                {
                    _propertyChangedAction.Invoke(propertyName);
                }
            });
        }

        private bool _isAddingTraceLocation;

        private bool IsAddingTraceLocation
        {
            get => _isAddingTraceLocation;
            set
            {
                if (_isAddingTraceLocation != value)
                {
                    _isAddingTraceLocation = value;
                    Status = GetStatusBasedOnSelection();
                }
            }
        }

        private async Task GetTraceConfigurationsAsync()
        {
            try
            {
                IsBusy = true;
                _traceConfigurations.Clear();

                if (GeoView is MapView mapView && mapView.Map is Map map && SelectedUtilityNetwork is UtilityNetwork utilityNetwork)
                {
                    var traceConfigurations = await map.GetNamedTraceConfigurationsFromUtilityNetworkAsync(utilityNetwork);
                    foreach (var traceConfiguration in traceConfigurations)
                    {
                        _traceConfigurations.Add(traceConfiguration);
                    }
                }

                if (_traceConfigurations.Count == 1)
                {
                    SelectedTraceConfiguration = _traceConfigurations[0];
                }
                else
                {
                    Status = GetStatusBasedOnSelection();
                }
            }
            catch (Exception ex)
            {
                Status = $"Loading trace configurations failed ({ex.GetType().Name}):{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }

            if (TraceConfigurationsChanged != null)
            {
                TraceConfigurationsChanged.Invoke(this, new TraceConfigurationsChangedEventArgs(_traceConfigurations));
            }
        }

        private void ClearResults()
        {
            _resultGraphicsOverlay.Graphics.Clear();
            _traceFunctionResults.Clear();

            if (GeoView is MapView mapView && mapView.Map is Map map && map.OperationalLayers != null)
            {
                foreach (var layer in map.OperationalLayers)
                {
                    if (layer is FeatureLayer featureLayer)
                    {
                        featureLayer.ClearSelection();
                    }
                }
            }
        }

        private void Reset(bool clearUtilityNetwork = false)
        {
            if (clearUtilityNetwork)
            {
                _utilityNetworks.Clear();
                SelectedUtilityNetwork = null;
                _traceConfigurations.Clear();
                SelectedTraceConfiguration = null;
            }

            TraceParameters = null;
            _startingLocations.Clear();
            _traceLocationsGraphicsOverlay.Graphics.Clear();

            ClearResults();

            Status = GetStatusBasedOnSelection();
        }

        private async Task TraceAsync()
        {
            Exception error = null;
            var traceResults = Enumerable.Empty<UtilityTraceResult>();

            try
            {
                Status = "Running a trace...";
                IsBusy = true;

                ClearResults();

                if (SelectedUtilityNetwork == null)
                {
                    throw new InvalidOperationException("No utility network selected.");
                }

                if (SelectedTraceConfiguration == null)
                {
                    throw new InvalidOperationException("No trace configuration selected.");
                }

                if (TraceParameters == null)
                {
                    TraceParameters = new UtilityTraceParameters(SelectedTraceConfiguration, _startingLocations);
                }
                else if (TraceParameters.StartingLocations.Count == 0)
                {
                    foreach (var startingLocation in _startingLocations)
                    {
                        TraceParameters.StartingLocations.Add(startingLocation);
                    }
                }

                traceResults = await SelectedUtilityNetwork.TraceAsync(TraceParameters);

                Envelope elementExtent = null;

                foreach (var traceResult in traceResults)
                {
                    if (traceResult.Warnings.Count > 0)
                    {
                        Status = $"Trace warnings: {string.Join("\n", traceResult.Warnings)}";
                    }

                    if (traceResult is UtilityElementTraceResult elementTraceResult)
                    {
                        Status = $"'{elementTraceResult.Elements.Count}' element(s) found.";
                        var features = await SelectedUtilityNetwork.GetFeaturesForElementsAsync(elementTraceResult.Elements);
                        Status = $"Selecting '{features.Count()}' feature(s).";

                        bool getElementExtent = AutoZoomToTraceResults && !traceResults.Any(r => r is UtilityGeometryTraceResult);

                        if (GeoView is MapView mapView && mapView.Map is Map map && map.OperationalLayers != null)
                        {
                            int selected = 0;
                            foreach (var featureLayer in GetFeatureLayer(map.OperationalLayers))
                            {
                                var featuresInLayer = features.Where(f => f.FeatureTable == featureLayer.FeatureTable);

                                if (getElementExtent)
                                {
                                    foreach (var feature in featuresInLayer)
                                    {
                                        if (feature.Geometry?.Extent is Envelope extent)
                                        {
                                            if (elementExtent == null)
                                            {
                                                elementExtent = extent;
                                            }
                                            else
                                            {
                                                if (elementExtent.SpatialReference?.IsEqual(extent.SpatialReference) == false
                                                    && GeometryEngine.Project(extent, elementExtent.SpatialReference) is Envelope projectedExtent)
                                                {
                                                    extent = projectedExtent;
                                                }

                                                if (GeometryEngine.CombineExtents(elementExtent, extent) is Envelope combinedExtents)
                                                {
                                                    elementExtent = combinedExtents;
                                                }
                                            }
                                        }
                                    }
                                }

                                featureLayer.SelectFeatures(featuresInLayer);
                                var selectedFeatures = await featureLayer.GetSelectedFeaturesAsync();
                                selected += selectedFeatures.Count();
                            }

                            Status = $"'{features.Count()}' feature(s) selected.";
                        }
                    }
                    else if (traceResult is UtilityGeometryTraceResult geometryTraceResult)
                    {
                        if (geometryTraceResult.Multipoint is Multipoint multipoint)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(multipoint, ResultPointSymbol ?? _defaultResultPointSymbol));
                        }

                        if (geometryTraceResult.Polyline is Polyline polyline)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(polyline, ResultLineSymbol ?? _defaultResultLineSymbol));
                        }

                        if (geometryTraceResult.Polygon is Polygon polygon)
                        {
                            _resultGraphicsOverlay.Graphics.Add(new Graphic(polygon, ResultFillSymbol ?? _defaultResultFillSymbol));
                        }

                        Status = $"'{_resultGraphicsOverlay.Graphics.Count}' aggregated geometries found.";
                    }
                    else if (traceResult is UtilityFunctionTraceResult functionTraceResult)
                    {
                        foreach (var functionOutput in functionTraceResult.FunctionOutputs)
                        {
                            _traceFunctionResults.Add(functionOutput);
                        }

                        Status = $"'{_traceFunctionResults.Count}' function result(s) found.";
                    }
                }

                var resultExtent = elementExtent ?? _resultGraphicsOverlay.Extent;
                if (AutoZoomToTraceResults && GeoView is GeoView geoView && resultExtent?.IsEmpty == false &&
                    GeometryEngine.Buffer(resultExtent, 15) is Geometry.Geometry bufferedGeometry)
                {
                    _ = geoView.SetViewpointAsync(new Viewpoint(bufferedGeometry));
                }
            }
            catch (Exception ex)
            {
                Status = $"Running a trace failed ({ex.GetType().Name}): {ex.Message}";
                error = ex;
            }
            finally
            {
                IsBusy = false;
            }

            if (TraceCompleted != null)
            {
                if (error is Exception traceError)
                {
                    TraceCompleted.Invoke(this, new TraceCompletedEventArgs(TraceParameters, traceError));
                }
                else
                {
                    TraceCompleted.Invoke(this, new TraceCompletedEventArgs(TraceParameters, traceResults));
                }
            }
        }

        private IEnumerable<FeatureLayer> GetFeatureLayer(IEnumerable<Layer> layers)
        {
            foreach (var layer in layers)
            {
                if (layer is FeatureLayer featureLayer)
                {
                    yield return featureLayer;
                }

                if (layer is GroupLayer groupLayer)
                {
                    GetFeatureLayer(groupLayer.Layers);
                }
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        private void UpdateGeoView(GeoView oldGeoView, GeoView newGeoView)
        {
            Reset(true);

#if !XAMARIN && !XAMARIN_FORMS
            if (oldGeoView is MapView oldMapView)
            {
#if NETFX_CORE
                oldMapView.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                BindablePropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(oldMapView, OnGeoModelChanged);
#endif
            }
            else if (oldGeoView is SceneView oldSceneView)
            {
#if NETFX_CORE
                oldSceneView.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#else
                BindablePropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(oldSceneView, OnGeoModelChanged);
#endif
            }
#else
            if (oldGeoView is INotifyPropertyChanged oldNotifyPropChanged)
            {
                oldNotifyPropChanged.PropertyChanged -= OnGeoViewPropertyChanged;
            }
#endif
            if (oldGeoView != null)
            {
                foreach (var overlay in new[] { _traceLocationsGraphicsOverlay, _resultGraphicsOverlay })
                {
                    if (oldGeoView.GraphicsOverlays.Contains(overlay))
                    {
                        oldGeoView.GraphicsOverlays.Remove(overlay);
                    }
                }

                oldGeoView.GeoViewTapped -= OnGeoViewTapped;
            }

#if !XAMARIN && !XAMARIN_FORMS
            if (newGeoView is MapView newMapView)
            {
#if NETFX_CORE
                _propertyChangedCallbackToken = newMapView.RegisterPropertyChangedCallback(MapView.MapProperty, OnGeoModelChanged);
#else
                BindablePropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(newMapView, OnGeoModelChanged);
#endif
            }
            else if (newGeoView is SceneView newSceneView)
            {
#if NETFX_CORE
                _propertyChangedCallbackToken = newSceneView.RegisterPropertyChangedCallback(SceneView.SceneProperty, OnGeoModelChanged);
#else
                BindablePropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(newSceneView, OnGeoModelChanged);
#endif
            }
#else

            if (newGeoView is INotifyPropertyChanged newNotifyPropChanged)
            {
                newNotifyPropChanged.PropertyChanged += OnGeoViewPropertyChanged;
            }
#endif
            if (newGeoView != null)
            {
                foreach (var overlay in new[] { _traceLocationsGraphicsOverlay, _resultGraphicsOverlay })
                {
                    if (!newGeoView.GraphicsOverlays.Contains(overlay))
                    {
                        newGeoView.GraphicsOverlays.Add(overlay);
                    }
                }

                newGeoView.GeoViewTapped += OnGeoViewTapped;
            }

            // Handle case where geoview loads map while events are being set up
            OnGeoModelChanged(null, null);
        }

        private async void OnGeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (e.Handled || !IsAddingTraceLocation || SelectedUtilityNetwork == null)
            {
                return;
            }

            try
            {
                Status = "Identifying a starting location...";
                IsBusy = true;

                if (sender is GeoView geoView)
                {
                    var identifyResults = await geoView.IdentifyLayersAsync(e.Position, 5d, false);
                    if (GetFeature(identifyResults) is ArcGISFeature feature && SelectedUtilityNetwork.CreateElement(feature) is UtilityElement element)
                    {
                        if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Edge && feature.Geometry is Polyline polyline)
                        {
                            Status = "Computing fraction along edge...";
                            if (polyline.HasZ && GeometryEngine.RemoveZ(polyline) is Polyline polyline2d)
                            {
                                polyline = polyline2d;
                            }

                            if (e.Location.SpatialReference.IsEqual(polyline?.SpatialReference) == false
                                && GeometryEngine.Project(polyline, e.Location.SpatialReference) is Polyline projectedPolyline)
                            {
                                polyline = projectedPolyline;
                            }

                            if (GeometryEngine.FractionAlong(polyline, e.Location, double.NaN) is double fractionAlongEdge
                                && !double.IsNaN(fractionAlongEdge))
                            {
                                element.FractionAlongEdge = fractionAlongEdge;
                            }
                        }
                        else if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Junction && element.AssetType?.TerminalConfiguration?.Terminals.Count > 1)
                        {
                            Status = "Selecting a terminal...";
                            element = await GetElementWithTerminalAsync(e.Location, element);
                        }

                        if (TraceLocationTapped != null)
                        {
                            TraceLocationTapped.Invoke(this, new TraceLocationTappedEventArgs(element));
                        }

                        _traceLocationsGraphicsOverlay.Graphics.Add(new Graphic(feature.Geometry as MapPoint ?? e.Location, StartingLocationSymbol ?? _defaultStartingLocationSymbol));
                        _startingLocations.Add(element);

                        Status = GetStatusBasedOnSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                Status = $"Identifying a starting location failed ({ex.GetType().Name}): {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ArcGISFeature GetFeature(IEnumerable<IdentifyLayerResult> layerResults)
        {
            foreach (var layerResult in layerResults)
            {
                if (GetFeature(layerResult) is ArcGISFeature element)
                {
                    return element;
                }
            }

            return null;
        }

        private ArcGISFeature GetFeature(IdentifyLayerResult layerResult)
        {
            foreach (var geoElement in layerResult.GeoElements)
            {
                if (geoElement is ArcGISFeature feature)
                {
                    return feature;
                }
            }

            return GetFeature(layerResult.SublayerResults);
        }

#if XAMARIN || XAMARIN_FORMS
        private void OnGeoViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                OnGeoModelChanged(sender, e);
            }
        }
#endif

        private void OnGeoModelChanged(object sender, object e)
        {
            Reset(true);

            var geoModel = sender is MapView mapView && mapView.Map is ILoadable mapLoadable ? mapLoadable :
                (sender is SceneView scenView && scenView.Scene is ILoadable sceneLoadable ? sceneLoadable : null);
            if (geoModel is ILoadable loadable)
            {
                // Listen for load completion
                var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(loadable)
                {
                    OnEventAction = (instance, source, eventArgs) => OnGeoModelLoaded(source, eventArgs),
                    OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent,
                };
                loadable.Loaded += listener.OnEvent;

                // Ensure event is raised even if already loaded
                _ = loadable.RetryLoadAsync();
            }
        }

        private void OnUtilityNetworkCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    Reset(true);
                }

                if (e.OldItems != null)
                {
                    foreach (UtilityNetwork utilityNetwork in e.OldItems)
                    {
                        if (SelectedUtilityNetwork == utilityNetwork)
                        {
                            Reset(true);
                        }

                        _utilityNetworks.Remove(utilityNetwork);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (UtilityNetwork utilityNetwork in e.NewItems)
                    {
                        _utilityNetworks.Add(utilityNetwork);
                    }
                }
            });
        }

        private void OnGeoModelLoaded(object sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                try
                {
                    IsBusy = true;
                    _utilityNetworks.Clear();

                    var utilityNetworks = (sender is Map map ? map.UtilityNetworks : null) ?? throw new ArgumentException("No UtilityNetworks found.");

                    if (utilityNetworks is INotifyCollectionChanged incc)
                    {
                        var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(incc)
                        {
                            OnEventAction = (instance, source, eventArgs) => OnUtilityNetworkCollectionChanged(sender, eventArgs),
                            OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
                        };
                        incc.CollectionChanged += listener.OnEvent;
                    }

                    foreach (var utilityNetwork in utilityNetworks)
                    {
                        _utilityNetworks.Add(utilityNetwork);
                    }

                    if (_utilityNetworks.Count == 1)
                    {
                        SelectedUtilityNetwork = _utilityNetworks[0];
                    }
                    else
                    {
                        Status = GetStatusBasedOnSelection();
                    }
                }
                catch (Exception ex)
                {
                    Status = $"Loading utility networks failed ({ex.GetType().Name}): {ex.Message}";
                }
            });
        }

        private void RunOnUIThread(Action action)
        {
            _synchronizationContext?.Post((o) => action?.Invoke(), null);
        }

        private void UpdateTraceLocationSymbol(Symbol symbol)
        {
            foreach (var graphic in _traceLocationsGraphicsOverlay.Graphics)
            {
                graphic.Symbol = symbol;
            }
        }

        private void UpdateResultSymbol(Symbol symbol, GeometryType geometryType)
        {
            foreach (var graphic in _resultGraphicsOverlay.Graphics)
            {
                if (graphic.Geometry?.GeometryType == geometryType)
                {
                    graphic.Symbol = symbol;
                }
            }
        }

        private class DelegateCommand : ICommand
        {
            private readonly Action<object> _onExecute;

            internal DelegateCommand(Action<object> onExecute)
            {
                _onExecute = onExecute ?? throw new ArgumentNullException(nameof(onExecute));
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) => _onExecute?.Invoke(parameter);
        }

        private class TerminalPickerModel
        {
            internal TerminalPickerModel(UtilityElement element, DelegateCommand selectCommand)
            {
                Element = element;
                SelectCommand = selectCommand;
            }

            public UtilityElement Element { get; }

            public ICommand SelectCommand { get; }
        }
    }
}