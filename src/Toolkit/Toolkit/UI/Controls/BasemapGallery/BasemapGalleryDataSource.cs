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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Modifiable, observable collection of basemaps. Note, collection contents will be reset when a portal is set.
    /// </summary>
    public class BasemapGalleryDataSource : IList<BasemapGalleryItem>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private BasemapGalleryItem _activeItem;
        private GeoView _geoview;
        private ArcGISPortal _portal;
        private CancellationTokenSource _cancellationSource;
        private ArcGISPortal _bakedInPortal;
        private const string _portalUri = "a25523e2241d4ff2bcc9182cc971c156";

        private readonly ObservableCollection<BasemapGalleryItem> _internalList = new ObservableCollection<BasemapGalleryItem>();

        public BasemapGalleryDataSource()
        {
            // Load from default list
            _cancellationSource = new CancellationTokenSource();
            _ = ConfigureFromDefaultList(_cancellationSource.Token);

            _internalList.CollectionChanged += _internalList_CollectionChanged;
        }

        private void _internalList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        // Setting the portal sets the list
        public ArcGISPortal Portal
        {
            get => _portal;
            set
            {
                if (_portal != value)
                {
                    _portal = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Portal)));
                    if (_cancellationSource != null)
                    {
                        _cancellationSource.Cancel();
                    }

                    _cancellationSource = new CancellationTokenSource();
                    _ = UpdateForCurrentPortal(_portal, _cancellationSource.Token);
                }
            }
        }

        // Listens for geoview changes and map changes; refreshes each item if spatial reference changes
        public GeoView GeoView
        {
            get => _geoview;

            set
            {
                if (_geoview != value)
                {
                    if (_geoview != null)
                    {
                        _geoview.SpatialReferenceChanged -= _geoview_SpatialReferenceChanged;
                    }

                    _geoview = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoView)));

                    if (_geoview != null)
                    {
                        _geoview.SpatialReferenceChanged += _geoview_SpatialReferenceChanged;
                        // TODO - handle map changes; need to listen to map for basemap changes
                    }
                    HandleSpatialReferenceChanged(_geoview?.SpatialReference);
                }
            }
        }

        private void HandleSpatialReferenceChanged(SpatialReference sr)
        {
            _internalList?.ToList().ForEach(item => _ = item.NotifySpatialReferenceChanged(sr));
        }

        private void _geoview_SpatialReferenceChanged(object sender, EventArgs e)
        {
            HandleSpatialReferenceChanged(_geoview?.SpatialReference);
        }

        // When this is set, map/scene is updated.
        public BasemapGalleryItem SelectedBasemap
        {
            get => _activeItem;

            set
            {
                if (_activeItem != value)
                {
                    _activeItem = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBasemap)));

                    if (_activeItem?.Basemap != null && GeoView is MapView mv)
                    {
                        if (mv.Map == null)
                        {
                            mv.Map = new Map(_activeItem.Basemap);
                        }
                        else
                        {
                            mv.Map.Basemap = _activeItem.Basemap;
                        }
                    }
                    else if (_activeItem?.Basemap != null && GeoView is SceneView sv)
                    {
                        if (sv.Scene == null)
                        {
                            sv.Scene = new Scene(_activeItem.Basemap);
                        }
                        else
                        {
                            sv.Scene.Basemap = _activeItem.Basemap;
                        }
                    }
                }
            }
        }

        public int Count => _internalList?.Count() ?? 0;

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public object SyncRoot => throw new NotImplementedException();

        public bool IsSynchronized => false;

        object IList.this[int index] { get => _internalList[index]; set => _internalList[index] = (BasemapGalleryItem)value; }

        public BasemapGalleryItem this[int index] { get => _internalList[index]; set => _internalList[index] = value; }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private async Task UpdateForCurrentPortal(ArcGISPortal portal, CancellationToken token)
        {
            if (portal == null)
            {
                await ConfigureFromDefaultList(token);
                return;
            }

            Task<IEnumerable<Basemap>> getBasemapsTask;
            if (portal.PortalInfo.UseVectorBasemaps)
            {
                getBasemapsTask = portal.GetVectorBasemapsAsync(token);
            }
            else
            {
                getBasemapsTask = portal.GetBasemapsAsync(token);
            }

            var basemaps = await getBasemapsTask;

            _internalList.Clear();
            basemaps.Select(basemap => new BasemapGalleryItem(basemap)).ToList().ForEach(item => _internalList.Add(item));
        }

        private async Task ConfigureFromDefaultList(CancellationToken token)
        {
            if (_bakedInPortal == null)
            {
                _bakedInPortal = await ArcGISPortal.CreateAsync(token);
            }

            PortalGroup group = new PortalGroup(_bakedInPortal, _portalUri);

            token.ThrowIfCancellationRequested();

            await group.LoadAsync();

            token.ThrowIfCancellationRequested();

            if (group.LoadStatus != LoadStatus.Loaded) { return; }

            var searchParameters = PortalGroupContentSearchParameters.CreateForItemsOfType(PortalItemType.WebMap);

            var results = await group.FindItemsAsync(searchParameters, token);

            // TODO - should token be passed to gallery item to cancel loading?
            _internalList.Clear();

            results.Results.Select(res => new BasemapGalleryItem(new Basemap(res))).ToList().ForEach(item => _internalList.Add(item));
        }

        public int IndexOf(BasemapGalleryItem item) => _internalList?.IndexOf(item) ?? -1;

        public void Insert(int index, BasemapGalleryItem item) => _internalList?.Insert(index, item);

        public void RemoveAt(int index) => _internalList?.RemoveAt(index);

        public void Add(BasemapGalleryItem item) => _internalList?.Add(item);

        public void Clear() => _internalList.Clear();

        public bool Contains(BasemapGalleryItem item) => _internalList?.Contains(item) ?? false;

        public void CopyTo(BasemapGalleryItem[] array, int arrayIndex) => _internalList?.CopyTo(array, arrayIndex);

        public bool Remove(BasemapGalleryItem item) => _internalList?.Remove(item) ?? false;

        public IEnumerator<BasemapGalleryItem> GetEnumerator() => _internalList?.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _internalList?.GetEnumerator();

        int IList.Add(object value)
        {
            _internalList.Insert(0, (BasemapGalleryItem)value);
            return _internalList != null ? 0 : -1;
        }

        bool IList.Contains(object value) => _internalList?.Contains((BasemapGalleryItem)value) ?? false;

        int IList.IndexOf(object value) => _internalList?.IndexOf((BasemapGalleryItem)value) ?? -1;

        void IList.Insert(int index, object value) => _internalList?.Insert(index, (BasemapGalleryItem)value);

        void IList.Remove(object value) => _internalList?.Remove((BasemapGalleryItem)value);

        public void CopyTo(Array array, int index) => _internalList?.CopyTo((BasemapGalleryItem[])array, index);
    }
}
