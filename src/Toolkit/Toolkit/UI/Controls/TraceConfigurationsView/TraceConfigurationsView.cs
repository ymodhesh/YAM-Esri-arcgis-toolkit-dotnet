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
using System.ComponentModel;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UtilityNetworks;
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
    /// The TraceConfigurationsView view presents TraceConfigurations, either from a list defined by <see cref="TraceConfigurationsOverride" /> or
    /// the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    public partial class TraceConfigurationsView : Control
    {
        private TraceConfigurationsViewDataSource _dataSource = new TraceConfigurationsViewDataSource();

        /// <summary>
        /// Gets or sets the list of TraceConfigurations to display.
        /// Otherwise, the TraceConfigurations from the Map or Scene shown in the associated <see cref="GeoView" /> are displayed.
        /// </summary>
        /// <remarks>If set to a <see cref="System.Collections.Specialized.INotifyCollectionChanged" />, the view will be updated with collection changes.</remarks>
        public IEnumerable<UtilityNamedTraceConfiguration> TraceConfigurationsOverride
        {
            get => TraceConfigurationsOverrideImpl;
            set => TraceConfigurationsOverrideImpl = value;
        }

        /// <summary>
        /// Gets or sets the MapView or SceneView associated with this view. When a TraceConfiguration is selected, the viewpoint of this
        /// geoview will be set to the TraceConfiguration's viewpoint. By default, TraceConfigurations from the geoview's Map or Scene
        /// property will be shown. To show a custom TraceConfiguration list, set <see cref="TraceConfigurationsOverride" />.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get => GeoViewImpl;
            set => GeoViewImpl = value;
        }

        private void SelectAndNavigateToTraceConfiguration(UtilityNamedTraceConfiguration TraceConfiguration)
        {
            //if (TraceConfiguration?.Viewpoint == null)
            //{
            //    throw new ArgumentNullException("TraceConfiguration or TraceConfiguration viewpoint is null");
            //}

            //GeoView?.SetViewpointAsync(TraceConfiguration.Viewpoint);

            //TraceConfigurationSelected?.Invoke(this, TraceConfiguration);
        }

        /// <summary>
        /// Event raised when the user selects a TraceConfiguration.
        /// </summary>
        public event EventHandler<UtilityNamedTraceConfiguration> TraceConfigurationSelected;
    }
}