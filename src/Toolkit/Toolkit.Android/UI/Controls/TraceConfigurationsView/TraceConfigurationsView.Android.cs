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
using Android.Content;
using Android.Support.V7.Widget;
using Android.Util;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    public partial class TraceConfigurationsView
    {
        private RecyclerView _internalListView;
        private TraceConfigurationsAdapter _adapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceConfigurationsView"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        public TraceConfigurationsView(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceConfigurationsView"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc.</param>
        /// <param name="attr">The attributes of the AXML element declaring the view.</param>
        public TraceConfigurationsView(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        internal void Initialize()
        {
            _internalListView = new RecyclerView(Context);
            _internalListView.SetLayoutManager(new LinearLayoutManager(Context));
            _adapter = new TraceConfigurationsAdapter(Context, _traceConfigurations);
            _internalListView.SetAdapter(_adapter);
            AddView(_internalListView);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();

            if (_adapter != null)
            {
                _adapter.TraceConfigurationSelected += ListView_ItemClick;
            }
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();

            if (_adapter != null)
            {
                _adapter.TraceConfigurationSelected -= ListView_ItemClick;
            }
        }

        private void ListView_ItemClick(object sender, UtilityNamedTraceConfiguration traceConfiguration)
        {
            SelectedTraceConfiguration = traceConfiguration;
        }
    }
}