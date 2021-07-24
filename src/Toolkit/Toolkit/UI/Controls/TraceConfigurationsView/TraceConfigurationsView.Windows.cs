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
using System.Collections.Generic;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [TemplatePart(Name = "UtilityNetworkPicker", Type = typeof(ComboBox))]
    [TemplatePart(Name = "TraceConfigurationPicker", Type = typeof(ComboBox))]
    [TemplatePart(Name = "AddingLocationToggle", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "ResetButton", Type = typeof(Button))]
    [TemplatePart(Name = "TraceButton", Type = typeof(Button))]
    [TemplatePart(Name = "BusyIndicator", Type = typeof(ProgressBar))]
    [TemplatePart(Name = "StatusLabel", Type = typeof(TextBlock))]
    [TemplatePart(Name = "StartingLocationList", Type = typeof(ItemsControl))]
    [TemplatePart(Name = "FunctionResultList", Type = typeof(ItemsControl))]
    public partial class TraceConfigurationsView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceConfigurationsView"/> class.
        /// </summary>
        public TraceConfigurationsView()
        {
            _synchronizationContext = System.Threading.SynchronizationContext.Current ?? new System.Threading.SynchronizationContext();
            DefaultStyleKey = typeof(TraceConfigurationsView);
        }

        private ComboBox _utilityNetworkPicker;
        private ComboBox _traceConfigurationPicker;
        private ProgressBar _busyIndicator;
        private TextBlock _statusLabel;
        private ItemsControl _startingLocationModelList;
        private ItemsControl _functionResultList;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
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
                _utilityNetworkPicker.SelectionChanged += (s, e) => SelectedUtilityNetwork = (s as ComboBox)?.SelectedItem as UtilityNetwork;
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
                _traceConfigurationPicker.SelectionChanged += (s, e) => SelectedTraceConfiguration = (s as ComboBox)?.SelectedItem as UtilityNamedTraceConfiguration;
            }

            if (GetTemplateChild("AddingLocationToggle") is ToggleButton addingLocationToggle)
            {
                addingLocationToggle.Click += (s, e) => IsAddingStartingLocation = s is ToggleButton tb && tb.IsChecked.HasValue && tb.IsChecked.Value;
            }

            if (GetTemplateChild("ResetButton") is Button resetButton)
            {
                resetButton.Click += (s, e) => Reset();
            }

            if (GetTemplateChild("TraceButton") is Button traceButton)
            {
                traceButton.Click += (s, e) => _ = TraceAsync();
            }

            if (GetTemplateChild("BusyIndicator") is ProgressBar busyIndicator)
            {
                _busyIndicator = busyIndicator;
            }

            if (GetTemplateChild("StatusLabel") is TextBlock statusLabel)
            {
                _statusLabel = statusLabel;
            }

            if (GetTemplateChild("StartingLocationList") is ItemsControl startingLocationList)
            {
                _startingLocationModelList = startingLocationList;
                _startingLocationModelList.ItemsSource = _startingLocationModels;
                _startingLocationModels.CollectionChanged += (s, e) =>
                {
                    if (_startingLocationModelList != null)
                    {
                        _startingLocationModelList.Visibility = _startingLocationModels.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                        if (_startingLocationModelList.Visibility == Visibility.Visible)
                        {
                            if (_functionResultList != null)
                            {
                                _functionResultList.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                };
            }

            if (GetTemplateChild("FunctionResultList") is ItemsControl functionResultList)
            {
                _functionResultList = functionResultList;
                _functionResultList.ItemsSource = _traceFunctionResults;
                _traceFunctionResults.CollectionChanged += (s, e) =>
                {
                    if (_functionResultList != null)
                    {
                        _functionResultList.Visibility = _traceFunctionResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                        if (_functionResultList.Visibility == Visibility.Visible)
                        {
                            if (_startingLocationModelList != null)
                            {
                                _startingLocationModelList.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                };
            }

            _propertyChangedAction = new Action<string>((propertyName) =>
            {
                if (propertyName == nameof(IsBusy))
                {
                    if (_busyIndicator != null)
                    {
                        _busyIndicator.Visibility = IsBusy ? Visibility.Visible : Visibility.Collapsed;
                        _busyIndicator.IsIndeterminate = IsBusy;
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

            Status = GetCurrentState();
        }

        private GeoView GeoViewImpl
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateGeoView(e.OldValue as GeoView, e.NewValue as GeoView);
        }

        private IList<ArcGISFeature> StartingLocationsImpl
        {
            get { return (IList<ArcGISFeature>)GetValue(StartingLocationsProperty); }
            set { SetValue(StartingLocationsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocations" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationsProperty =
            DependencyProperty.Register(nameof(StartingLocations), typeof(IList<ArcGISFeature>), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnStartingLocationsPropertyChanged));

        private static void OnStartingLocationsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateStartingLocations(e.OldValue as IList<ArcGISFeature>, e.NewValue as IList<ArcGISFeature>);
        }

        private bool AutoZoomToTraceResultsImpl
        {
            get { return (bool)GetValue(AutoZoomProperty); }
            set { SetValue(AutoZoomProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoZoomToTraceResults" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoZoomProperty =
            DependencyProperty.Register(nameof(AutoZoomToTraceResults), typeof(bool), typeof(TraceConfigurationsView), new PropertyMetadata(true));

        private Symbol StartingLocationSymbolImpl
        {
            get { return (Symbol)GetValue(StartingLocationSymbolProperty); }
            set { SetValue(StartingLocationSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationSymbolProperty =
            DependencyProperty.Register(nameof(StartingLocationSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnStartingLocationSymbolPropertyChanged));

        private static void OnStartingLocationSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateStartingLocationSymbol(e.NewValue as Symbol);
        }

        private Symbol ResultPointSymbolImpl
        {
            get { return (Symbol)GetValue(ResultPointSymbolProperty); }
            set { SetValue(ResultPointSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultPointSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultPointSymbolProperty =
            DependencyProperty.Register(nameof(ResultPointSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnResultPointSymbolPropertyChanged));

        private static void OnResultPointSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateResultSymbol(e.NewValue as Symbol, GeometryType.Multipoint);
        }

        private Symbol ResultLineSymbolImpl
        {
            get { return (Symbol)GetValue(ResultLineSymbolProperty); }
            set { SetValue(ResultLineSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultLineSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultLineSymbolProperty =
            DependencyProperty.Register(nameof(ResultLineSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnResultLineSymbolPropertyChanged));

        private static void OnResultLineSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateResultSymbol(e.NewValue as Symbol, GeometryType.Polyline);
        }

        private Symbol ResultFillSymbolImpl
        {
            get { return (Symbol)GetValue(ResultFillSymbolProperty); }
            set { SetValue(ResultFillSymbolProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultFillSymbol" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultFillSymbolProperty =
            DependencyProperty.Register(nameof(ResultFillSymbol), typeof(Symbol), typeof(TraceConfigurationsView), new PropertyMetadata(null, OnResultFillSymbolPropertyChanged));

        private static void OnResultFillSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceConfigurationsView)d).UpdateResultSymbol(e.NewValue as Symbol, GeometryType.Polygon);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display each named trace configuration item.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the container element generated for each named trace configuration item.
        /// </summary>
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemContainerStyleProperty =
            DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null);

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display each starting location item.
        /// </summary>
        public DataTemplate StartingLocationItemTemplate
        {
            get { return (DataTemplate)GetValue(StartingLocationItemTemplateProperty); }
            set { SetValue(StartingLocationItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationItemTemplateProperty =
            DependencyProperty.Register(nameof(StartingLocationItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the container element generated for each starting location.
        /// </summary>
        public Style StartingLocationContainerStyle
        {
            get { return (Style)GetValue(StartingLocationContainerStyleProperty); }
            set { SetValue(StartingLocationContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="StartingLocationContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartingLocationContainerStyleProperty =
            DependencyProperty.Register(nameof(StartingLocationContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null);

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to display each trace function result item.
        /// </summary>
        public DataTemplate ResultItemTemplate
        {
            get { return (DataTemplate)GetValue(ResultItemTemplateProperty); }
            set { SetValue(ResultItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultItemTemplateProperty =
            DependencyProperty.Register(nameof(ResultItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="Style"/> that is applied to the container element generated for each trace function result item.
        /// </summary>
        public Style ResultItemContainerStyle
        {
            get { return (Style)GetValue(ResultItemContainerStyleProperty); }
            set { SetValue(ResultItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ResultItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResultItemContainerStyleProperty =
            DependencyProperty.Register(nameof(ResultItemContainerStyle), typeof(Style), typeof(TraceConfigurationsView), null);
    }
}
#endif