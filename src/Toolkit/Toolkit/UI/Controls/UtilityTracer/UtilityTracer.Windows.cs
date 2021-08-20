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

#if !XAMARIN

using System;
using System.Linq;
using Esri.ArcGISRuntime.UtilityNetworks;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Symbology;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Data;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Mapping;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Represents a control that enables user to view <see cref="UtilityElementTraceResult"/> and <see cref="UtilityFunctionTraceResult"/>.
    /// </summary>
    [TemplatePart(Name = "TraceConfigurationPicker", Type = typeof(ComboBox))]
    [TemplatePart(Name = "TraceButton", Type = typeof(Button))]
    public partial class UtilityTracer : Control
    {
        private ComboBox? _utilityNetworkPicker = null;
        private ComboBox? _traceConfigurationPicker = null;
        private Button? _traceButton = null;
        private ProgressBar? _busyIndicator = null;
        private TextBlock? _statusLabel = null;

        private ObservableCollection<UtilityNetwork> _utilityNetworks = new ObservableCollection<UtilityNetwork>();
        private ObservableCollection<UtilityNamedTraceConfiguration> _traceConfigurations = new ObservableCollection<UtilityNamedTraceConfiguration>();
        private ObservableCollection<UtilityElement> _startingLocations = new ObservableCollection<UtilityElement>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityTracer"/> class.
        /// </summary>
        public UtilityTracer()
        {
            DefaultStyleKey = typeof(UtilityTracer);
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("UtilityNetworkPicker") is ComboBox utilityNetworkPicker)
            {
                _utilityNetworkPicker = utilityNetworkPicker;
                _utilityNetworkPicker.ItemsSource = _utilityNetworks;
                _utilityNetworks.CollectionChanged += (s, e) =>
                {
                    if (_utilityNetworkPicker != null)
                    {
                        _utilityNetworkPicker.Visibility = _utilityNetworks.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
                _utilityNetworkPicker.SelectionChanged += (s, e) =>
                 {
                     if (_utilityNetworkPicker?.SelectedItem is UtilityNetwork utilityNetwork)
                     {
                         SelectedUtilityNetwork = utilityNetwork;
                     }
                 };
            }

            if (GetTemplateChild("TraceConfigurationPicker") is ComboBox traceConfigurationPicker)
            {
                _traceConfigurationPicker = traceConfigurationPicker;
                _traceConfigurationPicker.ItemsSource = _traceConfigurations;
                _traceConfigurations.CollectionChanged += (s, e) =>
                {
                    if (_traceConfigurationPicker != null)
                    {
                        _traceConfigurationPicker.Visibility = _traceConfigurations.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
                _traceConfigurationPicker.SelectionChanged += (s, e) =>
                {
                    if (_traceConfigurationPicker?.SelectedItem is UtilityNamedTraceConfiguration traceConfiguration)
                    {
                        SelectedTraceConfiguration = traceConfiguration;
                    }
                };
            }

            if (GetTemplateChild("TraceButton") is Button traceButton)
            {
                _traceButton = traceButton;
                _traceButton.Click += (s, e) => _ = RunTraceAsync();
            }

            if (GetTemplateChild("ResetButton") is Button resetButton)
            {
                resetButton.Click += (s, e) => Reset();
            }

            if (GetTemplateChild("BusyIndicator") is ProgressBar busyIndicator)
            {
                _busyIndicator = busyIndicator;
            }

            if (GetTemplateChild("StatusLabel") is TextBlock statusLabel)
            {
                _statusLabel = statusLabel;
            }
        }

        private UtilityNetwork? _selectedUtilityNetwork;

        private UtilityNetwork? SelectedUtilityNetwork
        {
            get => _selectedUtilityNetwork;
            set
            {
                if (_selectedUtilityNetwork != value)
                {
                    _selectedUtilityNetwork = value;
                }
            }
        }

        private UtilityNamedTraceConfiguration? _selectedTraceConfiguration;

        private UtilityNamedTraceConfiguration? SelectedTraceConfiguration
        {
            get => _selectedTraceConfiguration;
            set
            {
                if (_selectedTraceConfiguration != value)
                {
                    _selectedTraceConfiguration = value;
                    if (_selectedTraceConfiguration is UtilityNamedTraceConfiguration traceConfiguration)
                    {
                        TraceParameters = new UtilityTraceParameters(traceConfiguration, _startingLocations);
                        var requiredStartingLocationCount = traceConfiguration.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ? 2 : 1;
                        if (_startingLocations.Count < requiredStartingLocationCount)
                        {
                            SetStatus("Set 'StartingFeatures' or enable 'AddStartingLocation'");
                        }
                        else
                        {
                            SetStatus("Click 'Trace' button");
                        }
                    }
                }
            }
        }

        private UtilityTraceParameters? _traceParameters;

        private UtilityTraceParameters? TraceParameters
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

        private void SetStatus(string status, bool isBusy = false)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = status;
            }

            if (_busyIndicator != null)
            {
                _busyIndicator.IsIndeterminate = isBusy;
            }
        }

        private async Task LoadTracesAsync()
        {
            try
            {
                SetStatus("Loading utility networks...", true);
                _utilityNetworks.Clear();

                if (GeoView is MapView mapView && mapView.Map is Map map && map is ILoadable loadable)
                {
                    //var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(loadable)
                    //{
                    //    OnEventAction = (instance, source, eventArgs) => OnGeoModelLoaded(source, eventArgs),
                    //    OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent,
                    //};
                    //loadable.Loaded += listener.OnEvent;

                    if (SelectedUtilityNetwork is UtilityNetwork utilityNetwork)
                    {
                        var traceConfigurations = await map.GetNamedTraceConfigurationsFromUtilityNetworkAsync(utilityNetwork);

                        foreach (var traceConfiguration in traceConfigurations)
                        {
                            _traceConfigurations.Add(traceConfiguration);
                        }

                        if (_traceConfigurations.Count == 1)
                        {
                            SelectedTraceConfiguration = _traceConfigurations[0];
                        }
                        else
                        {
                            SetStatus("Select a trace...");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Failed loading traces.\n{ex.GetType().Name} : {ex.Message}");
            }
            finally
            {
                SetStatus(string.Empty);
            }
        }

        //private async Task LoadTracesAsync()
        //{
        //    try
        //    {
        //        SetStatus("Loading traces...", true);
        //        _traceConfigurations.Clear();

        //        if (GeoView is MapView mapView && mapView.Map is Map map)
        //        {
        //            if (SelectedUtilityNetwork is UtilityNetwork utilityNetwork)
        //            {
        //                var traceConfigurations = await map.GetNamedTraceConfigurationsFromUtilityNetworkAsync(utilityNetwork);

        //                foreach (var traceConfiguration in traceConfigurations)
        //                {
        //                    _traceConfigurations.Add(traceConfiguration);
        //                }

        //                if (_traceConfigurations.Count == 1)
        //                {
        //                    SelectedTraceConfiguration = _traceConfigurations[0];
        //                }
        //                else
        //                {
        //                    SetStatus("Select a trace...");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SetStatus($"Failed loading traces.\n{ex.GetType().Name} : {ex.Message}");
        //    }
        //    finally
        //    {
        //        SetStatus(string.Empty);
        //    }
        //}

        private async Task RunTraceAsync()
        {
            try
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void Reset(bool clearCache = false)
        {
            if(clearCache)
            {
                SelectedUtilityNetwork = null;
                _utilityNetworks.Clear();
            }
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityTracer control)
            {
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="GeoView"/> from which <see cref="UtilityNetwork"/> is defined.
        /// </summary>
        public GeoView? GeoView
        {
            get { return GetValue(GeoViewProperty) as GeoView; }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(UtilityTracer), new PropertyMetadata(null, OnDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets a collection of <see cref="ArcGISFeature"/> to use as <see cref="UtilityTraceParameters.StartingLocations"/>.
        /// </summary>
        public IList<ArcGISFeature>? StartingFeatures
        {
            get { return GetValue(StartingFeaturesProperty) as IList<ArcGISFeature>; }
            set { SetValue(StartingFeaturesProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingFeatures"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingFeaturesProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(IList<ArcGISFeature>), typeof(UtilityTracer), new PropertyMetadata(null, OnDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets a value indicating whether to automatically zoom to the trace result area.
        /// </summary>
        public bool AutoZoomToTraceResults
        {
            get { return (bool)GetValue(AutoZoomToTraceResultsProperty); }
            set { SetValue(AutoZoomToTraceResultsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoZoomToTraceResults"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoZoomToTraceResultsProperty =
            DependencyProperty.Register(nameof(AutoZoomToTraceResults), typeof(bool), typeof(UtilityTracer), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether to add starting location when <see cref="GeoView"/> is tapped.
        /// </summary>
        public bool AddStartingLocation
        {
            get { return (bool)GetValue(AddStartingLocationProperty); }
            set { SetValue(AddStartingLocationProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AddStartingLocation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AddStartingLocationProperty =
            DependencyProperty.Register(nameof(AddStartingLocation), typeof(bool), typeof(UtilityTracer), new PropertyMetadata(true));

        private static void OnSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityTracer control)
            {
                //control.Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> used to display <see cref="UtilityTraceParameters.StartingLocations"/>.
        /// </summary>
        public Symbol? StartingLocationSymbol
        {
            get { return GetValue(StartingLocationSymbolProperty) as Symbol; }
            set { SetValue(StartingLocationSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationSymbolProperty =
            DependencyProperty.Register(nameof(StartingLocationSymbol), typeof(Symbol), typeof(UtilityTracer), new PropertyMetadata(null, OnSymbolPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> used to display <see cref="UtilityGeometryTraceResult.Multipoint"/> result.
        /// </summary>
        public Symbol? ResultPointSymbol
        {
            get { return GetValue(ResultPointSymbolProperty) as Symbol; }
            set { SetValue(ResultPointSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultPointSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultPointSymbolProperty =
            DependencyProperty.Register(nameof(ResultPointSymbol), typeof(Symbol), typeof(UtilityTracer), new PropertyMetadata(null, OnSymbolPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> used to display <see cref="UtilityGeometryTraceResult.Polyline"/> result.
        /// </summary>
        public Symbol? ResultLineSymbol
        {
            get { return GetValue(ResultLineSymbolProperty) as Symbol; }
            set { SetValue(ResultLineSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultLineSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultLineSymbolProperty =
            DependencyProperty.Register(nameof(ResultLineSymbol), typeof(Symbol), typeof(UtilityTracer), new PropertyMetadata(null, OnSymbolPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> used to display <see cref="UtilityGeometryTraceResult.Polygon"/> result.
        /// </summary>
        public Symbol? ResultFillSymbol
        {
            get { return GetValue(ResultFillSymbolProperty) as Symbol; }
            set { SetValue(ResultFillSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultFillSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultFillSymbolProperty =
            DependencyProperty.Register(nameof(ResultFillSymbol), typeof(Symbol), typeof(UtilityTracer), new PropertyMetadata(null, OnSymbolPropertyChanged));
    }
}
#endif