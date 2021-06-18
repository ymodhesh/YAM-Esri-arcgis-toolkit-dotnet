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

#if !__IOS__ && !__ANDROID__
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Displays a collection of images representing basemaps from ArcGIS Online, a user-defined Portal, or a user-defined collection.
    /// </summary>
    /// <remarks>
    /// If connected to a GeoView, changing the basemap selection will change the connected Map or Scene's basemap.
    /// Only basemaps whose spatial reference matches the map or scene's spatial reference can be selected for display.
    /// </remarks>
    public class BasemapGallery : Control
    {
        private readonly BasemapGalleryDataSource _dataSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGallery"/> class.
        /// </summary>
        public BasemapGallery()
        {
            DefaultStyleKey = typeof(BasemapGallery);
            _dataSource = new BasemapGalleryDataSource();
            DataContext = this;
        }

        public BasemapGalleryDataSource Basemaps { get => _dataSource; }

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
        }

        /// <summary>
        /// Gets or sets the portal to use for displaying basemaps.
        /// </summary>
        public ArcGISPortal Portal
        {
            get { return (ArcGISPortal)GetValue(PortalProperty); }
            set { SetValue(PortalProperty, value); }
        }

        /// <summary>
        /// Gets or sets the connected GeoView.
        /// </summary>
        public GeoView GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        /// <summary>
        /// Gets or sets the style used for the container used to display items when showing basemaps in a list.
        /// </summary>
        public Style ListItemContainerStyle
        {
            get { return (Style)GetValue(ListItemContainerStyleProperty); }
            set { SetValue(ListItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the style used for the container used to display items when showing basemaps in a grid.
        /// </summary>
        public Style GridItemContainerStyle
        {
            get { return (Style)GetValue(GridItemContainerStyleProperty); }
            set { SetValue(GridItemContainerStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to show basemaps in a list.
        /// </summary>
        public DataTemplate ListItemTemplate
        {
            get { return (DataTemplate)GetValue(ListItemTemplateProperty); }
            set { SetValue(ListItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to show basemaps in a grid.
        /// </summary>
        public DataTemplate GridItemTemplate
        {
            get { return (DataTemplate)GetValue(GridItemTemplateProperty); }
            set { SetValue(GridItemTemplateProperty, value); }
        }

        private static void OnGeoViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BasemapGallery)d)._dataSource.GeoView = e.NewValue as GeoView;
        }

        private static void OnPortalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BasemapGallery)d)._dataSource.Portal = e.NewValue as ArcGISPortal;
        }

        /// <summary>
        /// Identifies the <see cref="ListItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ListItemContainerStyleProperty =
            DependencyProperty.Register(nameof(ListItemContainerStyle), typeof(Style), typeof(BasemapGallery), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="GridItemContainerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridItemContainerStyleProperty =
            DependencyProperty.Register(nameof(GridItemContainerStyle), typeof(Style), typeof(BasemapGallery), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ListItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ListItemTemplateProperty =
            DependencyProperty.Register(nameof(ListItemTemplate), typeof(DataTemplate), typeof(BasemapGallery), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="GridItemTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridItemTemplateProperty =
            DependencyProperty.Register(nameof(GridItemTemplate), typeof(DataTemplate), typeof(BasemapGallery), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="GeoView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(BasemapGallery), new PropertyMetadata(null, OnGeoViewPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Portal"/> dependency proeprty.
        /// </summary>
        public static readonly DependencyProperty PortalProperty =
            DependencyProperty.Register(nameof(Portal), typeof(ArcGISPortal), typeof(BasemapGallery), new PropertyMetadata(null, OnPortalPropertyChanged));
    }
}
#endif
