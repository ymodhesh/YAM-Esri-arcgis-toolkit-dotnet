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
using System.Linq;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Creates the UI for the list items in the associated list of TraceConfigurations.
    /// </summary>
    internal class TraceConfigurationsAdapter : RecyclerView.Adapter
    {
        private readonly ObservableCollection<UtilityNamedTraceConfiguration> _dataSource;
        private readonly Context _context;
        private List<UtilityNamedTraceConfiguration> _shadowList = new List<UtilityNamedTraceConfiguration>();

        internal TraceConfigurationsAdapter(Context context, ObservableCollection<UtilityNamedTraceConfiguration> dataSource)
        {
            _context = context;
            _dataSource = dataSource;
            _shadowList = dataSource.ToList();

            var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(dataSource)
            {
                OnEventAction = (instance, source, eventArgs) =>
                {
                    switch (eventArgs.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            _shadowList.InsertRange(eventArgs.NewStartingIndex, eventArgs.NewItems.OfType<UtilityNamedTraceConfiguration>());
                            NotifyItemInserted(eventArgs.NewStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            _shadowList.RemoveRange(eventArgs.OldStartingIndex, eventArgs.OldItems.Count);
                            NotifyItemRemoved(eventArgs.OldStartingIndex);
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            _shadowList = _dataSource.ToList();
                            NotifyDataSetChanged();
                            break;
                        case NotifyCollectionChangedAction.Move:
                            _shadowList = _dataSource.ToList();
                            NotifyDataSetChanged();
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            _shadowList[eventArgs.OldStartingIndex] = (UtilityNamedTraceConfiguration)eventArgs.NewItems[0];
                            NotifyItemChanged(eventArgs.OldStartingIndex);
                            break;
                    }
                },
                OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent,
            };

            dataSource.CollectionChanged += listener.OnEvent;
        }

        public override int ItemCount => _shadowList?.Count() ?? 0;

        /// <inheritdoc />
        public override long GetItemId(int position) => position;

        public event EventHandler<UtilityNamedTraceConfiguration> TraceConfigurationSelected;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            TraceConfigurationItemViewHolder configurationHolder = holder as TraceConfigurationItemViewHolder;
            if (_shadowList != null && _shadowList.Count() > position)
            {
                configurationHolder.TraceConfigurationLabel.Text = _shadowList.ElementAt(position).Name;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = new TraceConfigurationItemView(_context);
            return new TraceConfigurationItemViewHolder(itemView, OnTraceConfigurationClicked);
        }

        private void OnTraceConfigurationClicked(int position)
        {
            TraceConfigurationSelected?.Invoke(this, _shadowList.ElementAt(position));
        }
    }
}