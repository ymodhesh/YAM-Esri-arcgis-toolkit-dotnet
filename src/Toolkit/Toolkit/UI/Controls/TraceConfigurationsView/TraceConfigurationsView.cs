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
using System.Drawing;
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
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
#elif __IOS__

using Control = UIKit.UIViewController;

#elif __ANDROID__
using Android.App;
using Android.Views;
using Control = Android.Widget.FrameLayout;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Represents a control that enables a user to view and trace with a <see cref="UtilityNamedTraceConfiguration"/>.
    /// </summary>
    public partial class TraceConfigurationsView : Control
    {
        private readonly ObservableCollection<UtilityNetwork> _utilityNetworks
            = new ObservableCollection<UtilityNetwork>();

        private readonly ObservableCollection<UtilityNamedTraceConfiguration> _traceConfigurations
            = new ObservableCollection<UtilityNamedTraceConfiguration>();

        private readonly ObservableCollection<UtilityTraceFunctionOutput> _traceFunctionResults
            = new ObservableCollection<UtilityTraceFunctionOutput>();

        private readonly ObservableCollection<StartingLocationModel> _startingLocationModels
            = new ObservableCollection<StartingLocationModel>();

        private readonly Symbol _defaultStartingLocationSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross,
            Color.LimeGreen, 20d);

        private readonly Symbol _defaultResultPointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle,
            Color.Blue, 20d);

        private readonly Symbol _defaultResultLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dot, Color.Blue,
            5d);

        private readonly Symbol _defaultResultFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.ForwardDiagonal,
            Color.Blue, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 2d));

        private readonly GraphicsOverlay _startingLocationsGraphicsOverlay = new GraphicsOverlay();
        private readonly GraphicsOverlay _resultGraphicsOverlay = new GraphicsOverlay();

        private SynchronizationContext _synchronizationContext;

        /// <summary>
        /// Gets or sets the <see cref="GeoView"/> for which <see cref="UtilityNamedTraceConfiguration"/>s are defined.
        /// </summary>
        public GeoView GeoView
        {
            get => GeoViewImpl;
            set => GeoViewImpl = value;
        }

        /// <summary>
        /// Gets or sets a mutable list of <see cref="ArcGISFeature"/> used as starting locations in a trace.
        /// </summary>
        public IList<ArcGISFeature> StartingLocations
        {
            get => StartingLocationsImpl;
            set => StartingLocationsImpl = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically zoom to the trace result area.
        /// </summary>
        public bool AutoZoomToTraceResults
        {
            get => AutoZoomToTraceResultsImpl;
            set => AutoZoomToTraceResultsImpl = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> for displaying starting locations.
        /// </summary>
        public Symbol StartingLocationSymbol
        {
            get => StartingLocationSymbolImpl;
            set => StartingLocationSymbolImpl = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> for displaying aggregated multipoint geometry result.
        /// </summary>
        public Symbol ResultPointSymbol
        {
            get => ResultPointSymbolImpl;
            set => ResultPointSymbolImpl = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> for displaying aggregated polyline geometry result.
        /// </summary>
        public Symbol ResultLineSymbol
        {
            get => ResultLineSymbolImpl;
            set => ResultLineSymbolImpl = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="Symbol"/> for displaying aggregated polygon geometry result.
        /// </summary>
        public Symbol ResultFillSymbol
        {
            get => ResultFillSymbolImpl;
            set => ResultFillSymbolImpl = value;
        }

        /// <summary>
        /// Occurs when a <see cref="UtilityNetwork"/> is changed.
        /// </summary>
        public event EventHandler<UtilityNetworkChangedEventArgs> UtilityNetworkChanged;

        /// <summary>
        /// Occurs when <see cref="UtilityNetwork.TraceAsync(UtilityTraceParameters)"/> has completed.
        /// </summary>
        public event EventHandler<TraceCompletedEventArgs> TraceCompleted;

        private UtilityNetwork _selectedUtilityNetwork;

        /// <summary>
        /// Gets or sets the utility network to perform trace analysis.
        /// </summary>
        private UtilityNetwork SelectedUtilityNetwork
        {
            get => _selectedUtilityNetwork;
            set
            {
                if (_selectedUtilityNetwork != value)
                {
                    _selectedUtilityNetwork = value;

                    Status = GetCurrentState();

                    if (UtilityNetworkChanged != null)
                    {
                        UtilityNetworkChanged.Invoke(this, new UtilityNetworkChangedEventArgs(_selectedUtilityNetwork));
                    }

                    _ = GetTraceConfigurationsAsync();
                }
            }
        }

        private UtilityTraceParameters _traceParameters;

        private UtilityNamedTraceConfiguration _selectedTraceConfiguration;

        /// <summary>
        /// Gets or sets the trace configuration from which to perform trace analysis.
        /// </summary>
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
                        _traceParameters = new UtilityTraceParameters(namedTraceConfiguration, _startingLocationList);
                    }

                    Status = GetCurrentState();
                }
            }
        }

        /// <summary>
        /// Gets a description of the current state of the toolkit control.
        /// </summary>
        /// <returns>A description of the toolkit control's current state.</returns>
        private string GetCurrentState()
        {
            var stringBuilder = new StringBuilder();

            if (_utilityNetworks.Count == 0)
            {
                stringBuilder.AppendLine("Loading the utility networks...");
            }
            else if (SelectedUtilityNetwork == null && _utilityNetworks.Count > 1)
            {
                stringBuilder.AppendLine("Select a utility network.");
            }
            else if (_traceConfigurations.Count == 0)
            {
                stringBuilder.AppendLine("Loading the trace configurations...");
            }
            else if (SelectedTraceConfiguration == null && _traceConfigurations.Count > 1)
            {
                stringBuilder.AppendLine("Select a trace configuration.");
            }
            else
            {
                var minimum = SelectedTraceConfiguration == null ? 0 :
                    (SelectedTraceConfiguration.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ?
                    2 : 1);

                if (_traceParameters != null)
                {
                    stringBuilder.AppendLine($"This '{_traceParameters.TraceType}' trace requires at least '{minimum}'"
                        + " starting location(s).");
                }

                if (IsAddingStartingLocation)
                {
                    stringBuilder.AppendLine("Tap a feature to identify a starting location.");
                }
                else
                {
                    stringBuilder.AppendLine("Toggle on 'Add Starting Location' button.");
                }

                if (minimum > 0 && _startingLocationList.Count >= minimum)
                {
                    stringBuilder.AppendLine("Or click 'Trace' button.");
                }
            }

            return stringBuilder.ToString();
        }

        private string _status;

        /// <summary>
        /// Gets or sets a value describing status, error or information for this toolkit control.
        /// </summary>
        private string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isBusy;

        /// <summary>
        /// Gets or sets a value indicating whether a value indicating when the control is busy.
        /// </summary>
        private bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        private Action<string> _propertyChangedAction;

        /// <summary>
        /// Raises a callback to propagate property changes on toolkit control.
        /// </summary>
        /// <param name="propertyName">Property with value that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RunOnUIThread(() =>
            {
                if (_propertyChangedAction != null)
                {
                    _propertyChangedAction.Invoke(propertyName);
                }
            });
        }

        private bool _isAddingStartingLocation;

        /// <summary>
        /// Gets or sets a value indicating whether a value indicating whether to add starting locations interactively.
        /// </summary>
        private bool IsAddingStartingLocation
        {
            get => _isAddingStartingLocation;
            set
            {
                if (_isAddingStartingLocation != value)
                {
                    _isAddingStartingLocation = value;
                    Status = GetCurrentState();
                }
            }
        }

        /// <summary>
        /// Retrieves <see cref="UtilityNamedTraceConfiguration"/> for a <see cref="UtilityNetwork"/>.
        /// </summary>
        /// <returns>An asynchronous task representing retrieval of trace configurations.</returns>
        private async Task GetTraceConfigurationsAsync()
        {
            try
            {
                IsBusy = true;
                _traceConfigurations.Clear();

                if (GeoView is MapView mapView && mapView.Map is Map map &&
                    SelectedUtilityNetwork is UtilityNetwork utilityNetwork)
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
                    Status = GetCurrentState();
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
        }

        /// <summary>
        /// Clears the selection and graphics on <see cref="GeoView"/>.
        /// </summary>
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

        /// <summary>
        /// Resets the control and <see cref="GeoView"/>.
        /// </summary>
        /// <param name="clearCache">A value indicating whether to clear cached values for utility networks and trace
        /// configurations.</param>
        private void Reset(bool clearCache = false)
        {
            if (clearCache)
            {
                _utilityNetworks.Clear();
                SelectedUtilityNetwork = null;
                _traceConfigurations.Clear();
                SelectedTraceConfiguration = null;
            }

            _traceParameters = null;
            _startingLocationList.Clear();
            OnPropertyChanged(nameof(CanTrace));
            _startingLocationModels.Clear();
            _startingLocationsGraphicsOverlay.Graphics.Clear();

            ClearResults();

            Status = GetCurrentState();
        }

        /// <summary>
        /// Performs trace analysis using selected utility network, trace configuration and starting locations.
        /// </summary>
        /// <returns>A task that represents the asynchronous trace operation.</returns>
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

                if (_traceParameters.StartingLocations.Count == 0)
                {
                    foreach (var startingLocation in _startingLocationList)
                    {
                        _traceParameters.StartingLocations.Add(startingLocation);
                    }
                }

                traceResults = await SelectedUtilityNetwork.TraceAsync(_traceParameters);

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

                        bool getElementExtent = AutoZoomToTraceResults &&
                            !traceResults.Any(r => r is UtilityGeometryTraceResult);

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
                                                    && GeometryEngine.Project(extent, elementExtent.SpatialReference)
                                                    is Envelope projectedExtent)
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
                    TraceCompleted.Invoke(this, new TraceCompletedEventArgs(_traceParameters, traceError));
                }
                else
                {
                    TraceCompleted.Invoke(this, new TraceCompletedEventArgs(_traceParameters, traceResults));
                }
            }
        }

        /// <summary>
        /// Recursively gets the <see cref="FeatureLayer"/> from a list of layers or group layers.
        /// </summary>
        /// <param name="layers">Layers in <see cref="GeoView"/>.</param>
        /// <returns>An enumeration of <see cref="FeatureLayer"/> items.</returns>
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

        /// <summary>
        /// Updates toolkit control based on <see cref="GeoView"/> property.
        /// </summary>
        /// <param name="oldGeoView">The old <see cref="GeoView"/>.</param>
        /// <param name="newGeoView">The new <see cref="GeoView"/>.</param>
        private void UpdateGeoView(GeoView oldGeoView, GeoView newGeoView)
        {
            Reset(true);

#if !XAMARIN && !XAMARIN_FORMS
            if (oldGeoView is MapView oldMapView)
            {
#if NETFX_CORE
                oldMapView.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(oldMapView, OnGeoModelChanged);
#endif
            }
            else if (oldGeoView is SceneView oldSceneView)
            {
#if NETFX_CORE
                oldSceneView.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#else
                DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(oldSceneView, OnGeoModelChanged);
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
                foreach (var overlay in new[] { _startingLocationsGraphicsOverlay, _resultGraphicsOverlay })
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
                DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(newMapView, OnGeoModelChanged);
#endif
            }
            else if (newGeoView is SceneView newSceneView)
            {
#if NETFX_CORE
                _propertyChangedCallbackToken = newSceneView.RegisterPropertyChangedCallback(SceneView.SceneProperty, OnGeoModelChanged);
#else
                DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(newSceneView, OnGeoModelChanged);
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
                foreach (var overlay in new[] { _startingLocationsGraphicsOverlay, _resultGraphicsOverlay })
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

        /// <summary>
        /// Handles <see cref="GeoView.GeoViewTapped"/> event of <see cref="GeoView"/> property.
        /// </summary>
        /// <param name="sender">The <see cref="GeoView"/> property.</param>
        /// <param name="e">The data for <see cref="GeoView.GeoViewTapped"/> event.</param>
        private async void OnGeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!IsAddingStartingLocation || SelectedUtilityNetwork == null)
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
                    foreach (var feature in GetFeature(identifyResults))
                    {
                        if (SelectedUtilityNetwork.CreateElement(feature) is UtilityElement element)
                        {
                            if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Edge &&
                                feature.Geometry is Polyline polyline)
                            {
                                Status = "Computing fraction along edge...";
                                if (polyline.HasZ && GeometryEngine.RemoveZ(polyline) is Polyline polyline2d)
                                {
                                    polyline = polyline2d;
                                }

                                if (e.Location.SpatialReference.IsEqual(polyline?.SpatialReference) == false
                                    && GeometryEngine.Project(polyline, e.Location.SpatialReference)
                                    is Polyline projectedPolyline)
                                {
                                    polyline = projectedPolyline;
                                }

                                if (GeometryEngine.FractionAlong(polyline, e.Location, double.NaN)
                                    is double fractionAlongEdge && !double.IsNaN(fractionAlongEdge))
                                {
                                    element.FractionAlongEdge = fractionAlongEdge;
                                }
                            }
                            else if (element.NetworkSource.SourceType == UtilityNetworkSourceType.Junction &&
                                element.AssetType?.TerminalConfiguration?.Terminals.Count > 1)
                            {
                                element.Terminal = element.AssetType.TerminalConfiguration.Terminals[0];
                            }

                            AddStartingLocation(element, feature.Geometry as MapPoint ?? e.Location, displayInfo: true);
                        }
                    }
                }

                Status = GetCurrentState();
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

        /// <summary>
        /// Recursively gets the <see cref="ArcGISFeature"/> from an identify.
        /// </summary>
        /// <param name="layerResults">Result for layers or sublayers in <see cref="GeoView"/>.</param>
        /// <returns>An enumeration of <see cref="ArcGISFeature"/> items.</returns>
        private IEnumerable<ArcGISFeature> GetFeature(IEnumerable<IdentifyLayerResult> layerResults)
        {
            foreach (var layerResult in layerResults)
            {
                foreach (var feature in GetFeature(layerResult))
                {
                    yield return feature;
                }
            }
        }

        /// <summary>
        /// Recursively gets the <see cref="ArcGISFeature"/> from an identify.
        /// </summary>
        /// <param name="layerResult">Result for a specific layer in <see cref="GeoView"/>.</param>
        /// <returns>An enumeration of <see cref="ArcGISFeature"/> items.</returns>
        private IEnumerable<ArcGISFeature> GetFeature(IdentifyLayerResult layerResult)
        {
            foreach (var geoElement in layerResult.GeoElements)
            {
                if (geoElement is ArcGISFeature feature)
                {
                    yield return feature;
                }
            }

            foreach (var feature in GetFeature(layerResult.SublayerResults))
            {
                yield return feature;
            }
        }

#if XAMARIN || XAMARIN_FORMS

        /// <summary>
        /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of <see cref="GeoView"/> property.
        /// </summary>
        /// <param name="sender">The <see cref="GeoView"/> property.</param>
        /// <param name="e">The data for <see cref="INotifyPropertyChanged.PropertyChanged"/> event of <see cref="GeoView"/>
        /// property.</param>
        private void OnGeoViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                OnGeoModelChanged(sender, e);
            }
        }

#endif

        /// <summary>
        /// Handles the update on <see cref="GeoView"/> property's content.
        /// </summary>
        /// <param name="sender">The <see cref="GeoView"/> property.</param>
        /// <param name="e">The data for the changed event of <see cref="GeoView"/> property's content.</param>m>
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

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of <see cref="GeoView"/>'s content.
        /// </summary>
        /// <param name="sender">The <see cref="INotifyCollectionChanged"/> content of <see cref="GeoView"/>
        /// property.</param>
        /// <param name="e">The data for <see cref="INotifyCollectionChanged.CollectionChanged"/> event of
        /// <see cref="GeoView"/>'s content.</param>
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

        /// <summary>
        /// Handles the <see cref="ILoadable.Loaded"/> event of <see cref="GeoView"/>'s content.
        /// </summary>
        /// <param name="sender">The <see cref="ILoadable"/> content of <see cref="GeoView"/> property.</param>
        /// <param name="e">The data for <see cref="ILoadable.Loaded"/> event of <see cref="GeoView"/>'s content.</param>
        private void OnGeoModelLoaded(object sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                try
                {
                    IsBusy = true;
                    _utilityNetworks.Clear();

                    var utilityNetworks = (sender is Map map ? map.UtilityNetworks : null) ??
                    throw new ArgumentException("No UtilityNetworks found.");

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
                        Status = GetCurrentState();
                    }
                }
                catch (Exception ex)
                {
                    Status = $"Loading utility networks failed ({ex.GetType().Name}): {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Ensures action is invoked on UI thread.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        private void RunOnUIThread(Action action)
        {
            _synchronizationContext?.Post((o) => action?.Invoke(), null);
        }

        /// <summary>
        /// Updates starting locations for display and trace analysis.
        /// </summary>
        /// <param name="oldStartingLocations">The old <see cref="StartingLocations"/>.</param>
        /// <param name="newStartingLocations">The new <see cref="StartingLocations"/>.</param>
        private void UpdateStartingLocations(IList<ArcGISFeature> oldStartingLocations, IList<ArcGISFeature> newStartingLocations)
        {
            Reset(true);
            if (oldStartingLocations is INotifyCollectionChanged oldIncc)
            {
                oldIncc.CollectionChanged -= OnStartingLocationsCollectionChanged;
            }

            if (newStartingLocations is INotifyCollectionChanged newIncc)
            {
                newIncc.CollectionChanged += OnStartingLocationsCollectionChanged;
            }
        }

        private readonly List<UtilityElement> _startingLocationList = new List<UtilityElement>();

        private bool CanTrace
        {
            get
            {
                var minimum = SelectedTraceConfiguration == null ? 0 :
                (SelectedTraceConfiguration.MinimumStartingLocations == UtilityMinimumStartingLocations.Many ?
                2 : 1);
                return minimum > 0 && _startingLocationList.Count >= minimum;
            }
        }

        /// <summary>
        /// Adds starting location for display and trace analysis.
        /// Optionally, displaying its information on this control.
        /// </summary>
        /// <param name="element">A starting location.</param>
        /// <param name="location"><see cref="MapPoint"/> for the starting location.</param>
        /// <param name="displayInfo">Indicates whether to display information on this control.</param>
        private void AddStartingLocation(UtilityElement element, MapPoint location,
            bool displayInfo = false)
        {
            _startingLocationList.Add(element);
            OnPropertyChanged(nameof(CanTrace));

            var graphic = new Graphic(location,
                                StartingLocationSymbol ?? _defaultStartingLocationSymbol);
            graphic.Attributes["GlobalId"] = element.GlobalId;
            _startingLocationsGraphicsOverlay.Graphics.Add(graphic);

            if (displayInfo)
            {
                var startingLocationModel = new StartingLocationModel(element,
                   new DelegateCommand((o) =>
                   {
                       if (o is UtilityElement elementToSelect)
                       {
                           if (_startingLocationsGraphicsOverlay.Graphics.FirstOrDefault(g =>
                           g.Attributes.ContainsKey("GlobalId") &&
                           Guid.Equals((Guid)g.Attributes["GlobalId"], elementToSelect.GlobalId))
                           is Graphic graphicToSelect)
                           {
                               _startingLocationsGraphicsOverlay.ClearSelection();
                               _startingLocationsGraphicsOverlay.SelectGraphics(new[] { graphicToSelect });
                           }
                       }
                   }),
                   new DelegateCommand((o) =>
                   {
                       if (o is UtilityElement elementToDelete)
                       {
                           if (_startingLocationsGraphicsOverlay.Graphics.FirstOrDefault(g =>
                           g.Attributes.ContainsKey("GlobalId") &&
                           Guid.Equals((Guid)g.Attributes["GlobalId"], elementToDelete.GlobalId))
                           is Graphic graphicToDelete)
                           {
                               _startingLocationsGraphicsOverlay.Graphics.Remove(graphicToDelete);
                           }

                           if (_startingLocationModels.FirstOrDefault(s => s.Element == elementToDelete)
                           is StartingLocationModel startingLocationToDelete)
                           {
                               _startingLocationModels.Remove(startingLocationToDelete);
                           }
                       }
                   }));

                _startingLocationModels.Add(startingLocationModel);
            }
        }

        /// <summary>
        /// Creates and adds a starting location for display and trace analysis.
        /// </summary>
        /// <param name="feature">Feature associated to a starting location element.</param>
        private void AddStartingLocation(ArcGISFeature feature)
        {
            try
            {
                Status = "Adding a starting location...";
                IsBusy = true;

                if (feature == null)
                {
                    throw new ArgumentNullException(nameof(feature));
                }

                MapPoint location = null;
                if (feature.Geometry is MapPoint mapPoint)
                {
                    location = mapPoint;
                }
                else if (feature.Geometry is Multipart multipart && multipart.Parts.Count > 0
                    && multipart.Parts[0].StartPoint is MapPoint startPoint)
                {
                    location = startPoint;
                }

                if (location == null)
                {
                    throw new InvalidOperationException("Starting location cannot be determined.");
                }

                if (SelectedUtilityNetwork == null)
                {
                    throw new InvalidOperationException("No utility network is selected.");
                }

                var element = SelectedUtilityNetwork.CreateElement(feature);
                AddStartingLocation(element, location);
            }
            catch (Exception ex)
            {
                Status = $"Adding a starting location failed ({ex.GetType().Name}):{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Removes a starting location from display and trace analysis.
        /// </summary>
        /// <param name="feature">Feature associated to a starting location element.</param>
        private void RemoveStartingLocation(ArcGISFeature feature)
        {
            try
            {
                Status = "Removing a starting location...";
                IsBusy = true;

                if (feature == null)
                {
                    throw new ArgumentNullException(nameof(feature));
                }

                if (SelectedUtilityNetwork == null)
                {
                    throw new InvalidOperationException("No utility network is selected.");
                }

                if (SelectedUtilityNetwork.CreateElement(feature) is UtilityElement elementToRemove)
                {
                    if (_startingLocationsGraphicsOverlay.Graphics.FirstOrDefault(g =>
                    g.Attributes.ContainsKey("GlobalId") &&
                    Guid.Equals((Guid)g.Attributes["GlobalId"], elementToRemove.GlobalId))
                    is Graphic graphicToDelete)
                    {
                        _startingLocationsGraphicsOverlay.Graphics.Remove(graphicToDelete);
                    }
                }
            }
            catch (Exception ex)
            {
                Status = $"Removing a starting location failed ({ex.GetType().Name}):{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of
        /// <see cref="StartingLocations"/> property.
        /// </summary>
        /// <param name="sender">The starting locations collection.</param>
        /// <param name="e">The data for <see cref="INotifyCollectionChanged.CollectionChanged"/> event of
        /// <see cref="StartingLocations"/>.</param>
        private void OnStartingLocationsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    if (_traceParameters != null)
                    {
                        _traceParameters.StartingLocations.Clear();
                    }

                    _startingLocationsGraphicsOverlay.Graphics.Clear();
                    _startingLocationList.Clear();
                    OnPropertyChanged(nameof(CanTrace));
                }

                if (e.OldItems != null)
                {
                    foreach (ArcGISFeature featureToRemove in e.OldItems)
                    {
                        RemoveStartingLocation(featureToRemove);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (ArcGISFeature featureToAdd in e.NewItems)
                    {
                        AddStartingLocation(featureToAdd);
                    }
                }
            });
        }

        /// <summary>
        /// Updates symbology used for displaying starting locations.
        /// </summary>
        /// <param name="symbol">Represents a starting location.</param>
        private void UpdateStartingLocationSymbol(Symbol symbol)
        {
            foreach (var startingLocationGraphic in _startingLocationsGraphicsOverlay.Graphics)
            {
                startingLocationGraphic.Symbol = symbol;
            }
        }

        /// <summary>
        /// Updates symbology used for displaying aggregated geometry trace results.
        /// </summary>
        /// <param name="symbol">Represents an aggregated geometry.</param>
        /// <param name="geometryType">Geometry type for which to apply the symbol.</param>
        private void UpdateResultSymbol(Symbol symbol, GeometryType geometryType)
        {
            foreach (var resultGraphic in _resultGraphicsOverlay.Graphics)
            {
                if (resultGraphic.Geometry?.GeometryType == geometryType)
                {
                    resultGraphic.Symbol = symbol;
                }
            }
        }

        /// <summary>
        /// Defines a delegate for invoking a specific action through a command.
        /// </summary>
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

        /// <summary>
        /// Defines a model for updating starting locations through this toolkit control.
        /// </summary>
        private class StartingLocationModel
        {
            internal StartingLocationModel(UtilityElement element, DelegateCommand selectCommand,
                DelegateCommand deleteCommand)
            {
                Element = element;
                SelectCommand = selectCommand;
                DeleteCommand = deleteCommand;
            }

            public UtilityElement Element { get; }

            public ICommand SelectCommand { get; }

            public ICommand DeleteCommand { get; }
        }
    }
}