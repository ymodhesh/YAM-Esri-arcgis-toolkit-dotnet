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
    [TemplatePart(Name = "ResultList", Type = typeof(ListView))]
    public partial class UtilityTraceResultView : Control
    {
        private TextBlock? _resultWarnings = null;
        private ListView? _resultList = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityTraceResultView"/> class.
        /// </summary>
        public UtilityTraceResultView()
        {
            DefaultStyleKey = typeof(UtilityTraceResultView);
        }

        /// <inheritdoc />
#if NETFX_CORE
        protected override void OnApplyTemplate()
#else
        public override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("ResultWarnings") is TextBlock resultWarnings)
            {
                _resultWarnings = resultWarnings;
            }

            if (GetTemplateChild("ResultList") is ListView resultList)
            {
                _resultList = resultList;
            }
        }

        private void Refresh()
        {
            if (_resultList == null || TraceResult == null)
            {
                return;
            }

            try
            {
                if (_resultWarnings != null && TraceResult?.Warnings?.Count > 0)
                {
                    _resultWarnings.Text = $"WARNINGS: {string.Join(Environment.NewLine, TraceResult.Warnings)}";
                }

                if (TraceResult is UtilityElementTraceResult elementTraceResult)
                {
                    _resultList.ItemTemplate = ElementTemplate;
                    _resultList.ItemsSource = elementTraceResult.Elements;
                }
                else if (TraceResult is UtilityFunctionTraceResult functionTraceResult)
                {
                    _resultList.ItemTemplate = FunctionTemplate;
                    _resultList.ItemsSource = functionTraceResult.FunctionOutputs;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UtilityTraceResultView control)
            {
                control.Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="UtilityTraceResult"/> to display.
        /// </summary>
        public UtilityTraceResult? TraceResult
        {
            get { return GetValue(TraceResultProperty) as UtilityTraceResult; }
            set { SetValue(TraceResultProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TraceResult"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TraceResultProperty =
            DependencyProperty.Register(nameof(TraceResult), typeof(UtilityTraceResult), typeof(UtilityTraceResultView), new PropertyMetadata(null, OnDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="ElementTemplate"/> used to display results for <see cref="UtilityElementTraceResult"/>.
        /// </summary>
        public DataTemplate? ElementTemplate
        {
            get { return GetValue(ElementTemplateProperty) as DataTemplate; }
            set { SetValue(ElementTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ElementTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElementTemplateProperty =
            DependencyProperty.Register(nameof(ElementTemplate), typeof(DataTemplate), typeof(UtilityTraceResultView), new PropertyMetadata(null, OnDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="FunctionTemplate"/> used to display results for <see cref="UtilityFunctionTraceResult"/>.
        /// </summary>
        public DataTemplate? FunctionTemplate
        {
            get { return GetValue(FunctionTemplateProperty) as DataTemplate; }
            set { SetValue(FunctionTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FunctionTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FunctionTemplateProperty =
            DependencyProperty.Register(nameof(FunctionTemplate), typeof(DataTemplate), typeof(UtilityTraceResultView), new PropertyMetadata(null, OnDependencyPropertyChanged));
    }
}
#endif