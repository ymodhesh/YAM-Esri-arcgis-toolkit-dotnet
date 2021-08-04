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
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UtilityNetworks;
#if NETFX_CORE
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A control that updates a <see cref="UtilityElement"/>.
    /// </summary>
    public partial class UtilityElementViewItem : Control
    {
        private void Initialize() => DefaultStyleKey = typeof(UtilityElementViewItem);

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();
        }

        /// <summary>
        /// Gets or sets the <see cref="ArcGISFeature"/> from which a <see cref="UtilityElement"/> is created.
        /// </summary>
        public ArcGISFeature? Feature
        {
            get { return GetValue(FeatureProperty) as ArcGISFeature; }
            set { SetValue(FeatureProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Feature"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FeatureProperty =
            DependencyProperty.Register(nameof(Feature), typeof(ArcGISFeature), typeof(UtilityElementViewItem), new PropertyMetadata(null, OnArcGISFeaturePropertyChanged));

        private static void OnArcGISFeaturePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UtilityElementViewItem)d;
        }

        /// <summary>
        /// Gets or sets the <see cref="UtilityNetwork"/> containing the <see cref="Feature"/>.
        /// </summary>
        public UtilityNetwork? UtilityNetwork
        {
            get { return GetValue(UtilityNetworkProperty) as UtilityNetwork; }
            set { SetValue(UtilityNetworkProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="UtilityNetwork"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UtilityNetworkProperty =
            DependencyProperty.Register(nameof(UtilityNetwork), typeof(UtilityNetwork), typeof(UtilityElementViewItem), new PropertyMetadata(null, OnUtilityNetworkPropertyChanged));

        private static void OnUtilityNetworkPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UtilityElementViewItem)d;
        }
    }
}
#endif