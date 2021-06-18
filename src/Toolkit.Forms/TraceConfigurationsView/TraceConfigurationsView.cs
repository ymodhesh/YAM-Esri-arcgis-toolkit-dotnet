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
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    /// <summary>
    /// The TraceConfigurationsView view presents TraceConfigurations, either from a list defined by <see cref="TraceConfigurationsOverride" /> or
    /// the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    public class TraceConfigurationsView : TemplatedView
    {
        private ListView _presentingView;
        private TraceConfigurationsViewDataSource _dataSource = new TraceConfigurationsViewDataSource();

        private static readonly DataTemplate DefaultDataTemplate;
        private static readonly ControlTemplate DefaultControlTemplate;

        static TraceConfigurationsView()
        {
            DefaultDataTemplate = new DataTemplate(() =>
            {
                var defaultCell = new TextCell();
                defaultCell.SetBinding(TextCell.TextProperty, nameof(TraceConfiguration.Name));
                return defaultCell;
            });

            string template = @"<ControlTemplate xmlns=""http://xamarin.com/schemas/2014/forms"" xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" xmlns:esriTK=""clr-namespace:Esri.ArcGISRuntime.Toolkit.Xamarin.Forms"">
                                    <ListView x:Name=""PresentingView"" HorizontalOptions=""FillAndExpand"" VerticalOptions=""FillAndExpand"">
                                        <x:Arguments>
                                            <ListViewCachingStrategy>RecycleElement</ListViewCachingStrategy>
                                        </x:Arguments>
                                    </ListView>
                                </ControlTemplate>";
            DefaultControlTemplate = Extensions.LoadFromXaml(new ControlTemplate(), template);
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

            if (_presentingView != null)
            {
                _presentingView.ItemSelected -= Internal_TraceConfigurationSelected;
            }

            _presentingView = GetTemplateChild("PresentingView") as ListView;

            if (_presentingView != null)
            {
                _presentingView.ItemSelected += Internal_TraceConfigurationSelected;
                _presentingView.ItemTemplate = ItemTemplate;
                _presentingView.ItemsSource = _dataSource;
            }
        }

        /// <summary>
        /// Gets or sets the data template that renders TraceConfiguration entries in the list.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the list of TraceConfigurations to display.
        /// Otherwise, the TraceConfigurations from the Map or Scene shown in the associated <see cref="GeoView" /> are displayed.
        /// </summary>
        /// <remarks>If set to a <see cref="System.Collections.Specialized.INotifyCollectionChanged" />, the view will be updated with collection changes.</remarks>
        /// <seealso cref="TraceConfigurationsOverrideProperty" />
        public IEnumerable<TraceConfiguration> TraceConfigurationsOverride
        {
            get { return (IEnumerable<TraceConfiguration>)GetValue(TraceConfigurationsOverrideProperty); }
            set { SetValue(TraceConfigurationsOverrideProperty, value); }
        }

        /// <summary>
        /// Gets or sets the geoview that contain the layers whose symbology and description will be displayed.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        /// <seealso cref="GeoViewProperty"/>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeoView"/> bindable property.
        /// </summary>
        public static readonly BindableProperty GeoViewProperty =
            BindableProperty.Create(nameof(GeoView), typeof(GeoView), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null, propertyChanged: GeoViewChanged);

        /// <summary>
        /// Identifies the <see cref="TraceConfigurationsOverride"/> bindable property.
        /// </summary>
        public static readonly BindableProperty TraceConfigurationsOverrideProperty =
            BindableProperty.Create(nameof(TraceConfigurationsOverride), typeof(IEnumerable<TraceConfiguration>), typeof(TraceConfigurationsView), null, BindingMode.OneWay, null, propertyChanged: TraceConfigurationsOverrideChanged);

        /// <summary>
        /// Identifies the <see cref="ItemTemplate" /> bindable property.
        /// </summary>
        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(TraceConfigurationsView), DefaultDataTemplate, BindingMode.OneWay, null, propertyChanged: ItemTemplateChanged);

        /// <summary>
        /// Handles property changes for the <see cref="TraceConfigurationsOverride" /> bindable property.
        /// </summary>
        private static void TraceConfigurationsOverrideChanged(BindableObject sender, object oldValue, object newValue)
        {
            ((TraceConfigurationsView)sender)._dataSource.SetOverrideList(newValue as IEnumerable<TraceConfiguration>);
        }

        /// <summary>
        /// Handles property changes for the <see cref="GeoView" /> bindable property.
        /// </summary>
        private static void GeoViewChanged(BindableObject sender, object oldValue, object newValue)
        {
            TraceConfigurationsView TraceConfigurationView = (TraceConfigurationsView)sender;

            TraceConfigurationView._dataSource.SetGeoView(newValue as GeoView);
        }

        /// <summary>
        /// Handles property changes for the <see cref="ItemTemplate" /> bindable property.
        /// </summary>
        private static void ItemTemplateChanged(BindableObject sender, object oldValue, object newValue)
        {
            TraceConfigurationsView TraceConfigurationView = (TraceConfigurationsView)sender;

            if (TraceConfigurationView?._presentingView != null)
            {
                TraceConfigurationView._presentingView.ItemTemplate = (DataTemplate)newValue;
            }
        }

        /// <summary>
        /// Selects the TraceConfiguration and navigates to it in the associated <see cref="GeoView" />.
        /// </summary>
        /// <param name="TraceConfiguration">TraceConfiguration to navigate to. Must be non-null with a valid viewpoint.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="TraceConfiguration"/> is <code>null</code>.</exception>
        private void SelectAndNavigateToTraceConfiguration(TraceConfiguration TraceConfiguration)
        {
            if (TraceConfiguration?.Viewpoint == null)
            {
                throw new ArgumentNullException("TraceConfiguration or TraceConfiguration viewpoint is null");
            }

            GeoView?.SetViewpointAsync(TraceConfiguration.Viewpoint);

            TraceConfigurationSelected?.Invoke(this, TraceConfiguration);
        }

        /// <summary>
        /// Handles selection on the underlying list view.
        /// </summary>
        private void Internal_TraceConfigurationSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is TraceConfiguration bm)
            {
                SelectAndNavigateToTraceConfiguration(bm);
            }

            if (e.SelectedItem != null)
            {
                ((ListView)sender).SelectedItem = null;
            }
        }

        /// <summary>
        /// Raised whenever a TraceConfiguration is selected.
        /// </summary>
        public event EventHandler<TraceConfiguration> TraceConfigurationSelected;
    }
}