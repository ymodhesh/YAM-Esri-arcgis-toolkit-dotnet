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

#if XAMARIN

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class TraceConfigurationsView
    {
        private void InitializeComponent()
        {
            _synchronizationContext = System.Threading.SynchronizationContext.Current ?? new System.Threading.SynchronizationContext();
            _propertyChangedAction = new Action<string>((propertyName) =>
            {
                if (propertyName == nameof(IsBusy))
                {
                    // TODO
                }
                else if (propertyName == nameof(Status))
                {
                    // TODO
                }
            });
        }

        private GeoView _geoView;

        private GeoView GeoViewImpl
        {
            get => _geoView;
            set
            {
                if (_geoView != value)
                {
                    var oldGeoView = _geoView;
                    _geoView = value;
                    UpdateGeoView(oldGeoView, _geoView);
                }
            }
        }

        private IList<ArcGISFeature> _startingLocations;

        private IList<ArcGISFeature> StartingLocationsImpl
        {
            get => _startingLocations;
            set
            {
                if (_startingLocations != value)
                {
                    var oldStartingLocations = _startingLocations;
                    _startingLocations = value;
                    UpdateStartingLocations(oldStartingLocations, _startingLocations);
                }
            }
        }

        private bool _autoZoomToTraceResults = true;

        private bool AutoZoomToTraceResultsImpl
        {
            get => _autoZoomToTraceResults;
            set => _autoZoomToTraceResults = value;
        }

        private Symbol _startingLocationSymbol;

        private Symbol StartingLocationSymbolImpl
        {
            get => _startingLocationSymbol;
            set
            {
                if (_startingLocationSymbol != value)
                {
                    _startingLocationSymbol = value;
                    UpdateStartingLocationSymbol(_startingLocationSymbol);
                }
            }
        }

        private Symbol _resultPointSymbol;

        private Symbol ResultPointSymbolImpl
        {
            get => _resultPointSymbol;
            set
            {
                if (_resultPointSymbol != value)
                {
                    _resultPointSymbol = value;
                    UpdateResultSymbol(_resultPointSymbol, GeometryType.Multipoint);
                }
            }
        }

        private Symbol _resultLineSymbol;

        private Symbol ResultLineSymbolImpl
        {
            get => _resultLineSymbol;
            set
            {
                if (_resultLineSymbol != value)
                {
                    _resultLineSymbol = value;
                    UpdateResultSymbol(_resultLineSymbol, GeometryType.Polyline);
                }
            }
        }

        private Symbol _resultFillSymbol;

        private Symbol ResultFillSymbolImpl
        {
            get => _resultFillSymbol;
            set
            {
                if (_resultFillSymbol != value)
                {
                    _resultFillSymbol = value;
                    UpdateResultSymbol(_resultFillSymbol, GeometryType.Polygon);
                }
            }
        }

        private Task<UtilityElement> GetElementWithTerminalAsync(MapPoint location, UtilityElement element)
        {
            var tcs = new TaskCompletionSource<UtilityElement>();

            // TODO
            return tcs.Task;
        }
    }
}

#endif