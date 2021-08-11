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
using Esri.ArcGISRuntime.UtilityNetworks;
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
    /// Represents a control that enables user to view and update properties of a <see cref="UtilityElement"/>.
    /// </summary>
    [TemplatePart(Name = "LabelControl", Type = typeof(ContentControl))]
    [TemplatePart(Name = "ContentControl", Type = typeof(ContentControl))]
    public partial class UtilityElementView : Control
    {
        private ContentControl? _labelControl = null;
        private ContentControl? _contentControl = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityElementView"/> class.
        /// </summary>
        public UtilityElementView()
        {
            DefaultStyleKey = typeof(UtilityElementView);
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("LabelControl") is ContentControl labelControl)
            {
                _labelControl = labelControl;
            }

            if (GetTemplateChild("ContentControl") is ContentControl contentControl)
            {
                _contentControl = contentControl;
            }
        }

        private async Task RefreshAsync()
        {
            if (_labelControl == null || _contentControl == null || Feature == null || UtilityNetwork == null)
            {
                return;
            }

            try
            {
                if (UtilityNetwork.LoadStatus != LoadStatus.Loaded)
                {
                    await UtilityNetwork.LoadAsync();
                }

                var element = UtilityNetwork.CreateElement(Feature);

                _labelControl.Content = element;
                _labelControl.ContentTemplate = LabelTemplate;

                _contentControl.Content = element;

                if (element?.NetworkSource?.SourceType == UtilityNetworkSourceType.Edge)
                {
                    _contentControl.ContentTemplate = EdgeTemplate;
                }
                else if (element?.AssetType?.TerminalConfiguration?.Terminals.Count > 1)
                {
                    _contentControl.ContentTemplate = JunctionTemplate;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityElementView control)
            {
                _ = control.RefreshAsync();
            }
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
            DependencyProperty.Register(nameof(Feature), typeof(ArcGISFeature), typeof(UtilityElementView), new PropertyMetadata(null, OnDependencyPropertyChanged));

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
            DependencyProperty.Register(nameof(UtilityNetwork), typeof(UtilityNetwork), typeof(UtilityElementView), new PropertyMetadata(null, OnDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="LabelTemplate"/> used to identify the <see cref="Feature"/> with the name of its network source and asset group.
        /// </summary>
        public DataTemplate? LabelTemplate
        {
            get { return GetValue(LabelTemplateProperty) as DataTemplate; }
            set { SetValue(LabelTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LabelTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelTemplateProperty =
            DependencyProperty.Register(nameof(LabelTemplate), typeof(DataTemplate), typeof(UtilityElementView), new PropertyMetadata(null, OnDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="JunctionTemplate"/> used to select a terminal for a junction <see cref="Feature"/> with multiple terminals.
        /// </summary>
        public DataTemplate? JunctionTemplate
        {
            get { return GetValue(JunctionTemplateProperty) as DataTemplate; }
            set { SetValue(JunctionTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="JunctionTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty JunctionTemplateProperty =
            DependencyProperty.Register(nameof(JunctionTemplate), typeof(DataTemplate), typeof(UtilityElementView), new PropertyMetadata(null, OnDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="EdgeTemplate"/> used to update the fraction along edge value for an edge <see cref="Feature"/>.
        /// </summary>
        public DataTemplate? EdgeTemplate
        {
            get { return GetValue(EdgeTemplateProperty) as DataTemplate; }
            set { SetValue(EdgeTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="EdgeTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EdgeTemplateProperty =
            DependencyProperty.Register(nameof(EdgeTemplate), typeof(DataTemplate), typeof(UtilityElementView), new PropertyMetadata(null, OnDependencyPropertyChanged));
    }
}
#endif