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
        private const string _portalUri = "a25523e2241d4ff2bcc9182cc971c156";
        private BasemapGalleryItem _activeItem;
        private GeoView _geoview;
        private ArcGISPortal _portal;
        private CancellationTokenSource _cancellationSource;
        private ArcGISPortal _bakedInPortal;

        private readonly ObservableCollection<BasemapGalleryItem> _internalList = new ObservableCollection<BasemapGalleryItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGalleryDataSource"/> class.
        /// </summary>
        public BasemapGalleryDataSource()
        {
            // Load from default list
            _cancellationSource = new CancellationTokenSource();
            _ = ConfigureFromDefaultList(_cancellationSource.Token);

            _internalList.CollectionChanged += InternalList_CollectionChanged;
        }

        /// <summary>
        /// Gets or sets the portal used to populate the list.
        /// </summary>
        /// <remarks>
        /// Setting this to a new portal will reset the contents of the list, including any custom additions.
        /// </remarks>
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

        /// <summary>
        /// Gets or sets a reference to the connected geoview.
        /// </summary>
        /// <remarks>
        /// The GeoView and any map or scene is observed for changes.
        /// Changes to the map or scene's spatial reference will change the validity of <see cref="BasemapGalleryItem"/> instances.
        /// Selection of a basemap via <see cref="SelectedBasemap"/> will change the map or scene's basemap. If the map or scene property is null, a new map or scene will be created with the selected basemap.
        /// </remarks>
        public GeoView GeoView
        {
            get => _geoview;

            set
            {
                if (_geoview != value)
                {
                    if (_geoview != null)
                    {
                        _geoview.SpatialReferenceChanged -= Geoview_SpatialReferenceChanged;
                    }

                    _geoview = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoView)));

                    if (_geoview != null)
                    {
                        _geoview.SpatialReferenceChanged += Geoview_SpatialReferenceChanged;
                        // TODO - handle map changes; need to listen to map for basemap changes
                    }

                    HandleSpatialReferenceChanged(_geoview?.SpatialReference);
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected basemap.
        /// </summary>
        /// <remarks>
        /// Setting this property will update the connected <see cref="GeoView"/>'s map or scene, or create a new map or scene if none is present.
        /// </remarks>
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

        /// <inheritdoc/>
        public BasemapGalleryItem this[int index] { get => _internalList[index]; set => _internalList[index] = value; }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
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
            _internalList?.Clear();

            results.Results.Select(res => new BasemapGalleryItem(new Basemap(res))).ToList().ForEach(item => _internalList.Add(item));
        }

        private void HandleSpatialReferenceChanged(SpatialReference sr) =>
            _internalList?.ToList().ForEach(item => _ = item.NotifySpatialReferenceChanged(sr));

        private void Geoview_SpatialReferenceChanged(object sender, EventArgs e) =>
            HandleSpatialReferenceChanged(_geoview?.SpatialReference);

        private void InternalList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            CollectionChanged?.Invoke(this, e);

#region interface implementation
        int ICollection<BasemapGalleryItem>.Count => _internalList?.Count() ?? 0;

        int ICollection.Count => _internalList?.Count() ?? 0;

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        bool ICollection<BasemapGalleryItem>.IsReadOnly => throw new NotImplementedException();

        object IList.this[int index] { get => _internalList[index]; set => _internalList[index] = (BasemapGalleryItem)value; }

        int IList<BasemapGalleryItem>.IndexOf(BasemapGalleryItem item) => _internalList?.IndexOf(item) ?? -1;

        void IList<BasemapGalleryItem>.Insert(int index, BasemapGalleryItem item) => _internalList?.Insert(index, item);

        void IList<BasemapGalleryItem>.RemoveAt(int index) => _internalList?.RemoveAt(index);

        void ICollection<BasemapGalleryItem>.Add(BasemapGalleryItem item) => _internalList?.Add(item);

        void ICollection<BasemapGalleryItem>.Clear() => _internalList.Clear();

        bool ICollection<BasemapGalleryItem>.Contains(BasemapGalleryItem item) => _internalList?.Contains(item) ?? false;

        void ICollection<BasemapGalleryItem>.CopyTo(BasemapGalleryItem[] array, int arrayIndex) => _internalList?.CopyTo(array, arrayIndex);

        bool ICollection<BasemapGalleryItem>.Remove(BasemapGalleryItem item) => _internalList?.Remove(item) ?? false;

        IEnumerator<BasemapGalleryItem> IEnumerable<BasemapGalleryItem>.GetEnumerator() => _internalList?.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _internalList?.GetEnumerator();

        int IList.Add(object value)
        {
            _internalList?.Insert(0, (BasemapGalleryItem)value);
            return _internalList != null ? 0 : -1;
        }

        bool IList.Contains(object value) => _internalList?.Contains((BasemapGalleryItem)value) ?? false;

        int IList.IndexOf(object value) => _internalList?.IndexOf((BasemapGalleryItem)value) ?? -1;

        void IList.Insert(int index, object value) => _internalList?.Insert(index, (BasemapGalleryItem)value);

        void IList.Remove(object value) => _internalList?.Remove((BasemapGalleryItem)value);

        void ICollection.CopyTo(Array array, int index) => _internalList?.CopyTo((BasemapGalleryItem[])array, index);

        void IList.Clear() => _internalList?.Clear();

        void IList.RemoveAt(int index) => _internalList?.RemoveAt(index);
#endregion interface implementation
    }
}
