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
using System.ComponentModel;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;

#if NETFX_CORE
using Windows.UI.Xaml.Media;
#elif NETFRAMEWORK || NETCOREAPP
using System.Windows.Media;
#elif XAMARIN
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// Encompasses an element in a basemap gallery.
    /// </summary>
    public class BasemapGalleryItem : INotifyPropertyChanged
    {
        private RuntimeImage _thumbnailOverride;
        #if NETFX_CORE || NETFRAMEWORK || NETCOREAPP
        private ImageSource _thumbnailImageSource;
        #endif
        private string _tooltipOverride;
        private string _nameOverride;
        private bool _isLoading;
        private bool _isValid;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGalleryItem"/> class.
        /// </summary>
        /// <param name="basemap">Basemap for this gallery item. Must be not null and loaded.</param>
        public BasemapGalleryItem(Basemap basemap)
        {
            Basemap = basemap;
            _ = LoadBasemapAsync();
        }

        private async Task LoadBasemapAsync()
        {
            IsLoading = true;
            try
            {
                if (Basemap != null && Basemap.LoadStatus != LoadStatus.Loaded)
                {
                    await Basemap.RetryLoadAsync();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Tooltip)));
                    await LoadImage();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadImage()
        {
            IsLoading = true;
            try
            {
                if (Thumbnail != null && Thumbnail.LoadStatus != LoadStatus.Loaded)
                {
                    await Thumbnail.RetryLoadAsync();
                }

                #if NETFRAMEWORK || NETFX_CORE || NETCOREAPP
                if (Thumbnail != null)
                {
                    ThumbnailImageSource = await Thumbnail.ToImageSourceAsync();
                }
                #endif
            }
            catch (Exception)
            {
                // Ignore
            }
            finally
            {
                IsLoading = false;
            }
        }

        internal async Task NotifySpatialReferenceChanged(SpatialReference sr)
        {
            if (sr == null)
            {
                IsValid = false;
                return;
            }

            await LoadBasemapAsync();

            var map = new Map(Basemap);
            await map.LoadAsync();
            if (map.LoadStatus != LoadStatus.Loaded)
            {
                IsValid = false;
            }

            IsValid = map.SpatialReference == sr;
        }

        /// <summary>
        /// Gets whether this basemap is a valid selection.
        /// </summary>
        public bool IsValid
        {
            get => _isValid; set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
                }
            }
        }

        /// <summary>
        /// Gets the basemap associated with this basemap item.
        /// </summary>
        public Basemap Basemap { get; private set; }

        /// <summary>
        /// Gets or sets the thumbnail to display for this basemap item.
        /// </summary>
        public RuntimeImage Thumbnail
        {
            get
            {
                if (_thumbnailOverride != null)
                {
                    return _thumbnailOverride;
                }

                return Basemap?.Item?.Thumbnail;
            }

            set
            {
                if (_thumbnailOverride != value)
                {
                    _thumbnailOverride = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail)));
                    _ = LoadImage();
                }
            }
        }

        #if NETCOREAPP || NETFX_CORE || NETFRAMEWORK
        public ImageSource ThumbnailImageSource
        {
            get => _thumbnailImageSource;
            private set
            {
                _thumbnailImageSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThumbnailImageSource)));
            }
        }
        #endif

        /// <summary>
        /// Gets or sets the tooltip to display for this basemap item.
        /// </summary>
        public string Tooltip
        {
            get
            {
                if (_tooltipOverride != null)
                {
                    return _tooltipOverride;
                }

                return Basemap.Item.Snippet;
            }

            set
            {
                if (_tooltipOverride != value)
                {
                    _tooltipOverride = value;
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Tooltip)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the name to display for this basemap item.
        /// </summary>
        public string Name
        {
            get
            {
                return _nameOverride ?? Basemap?.Name;
            }

            set
            {
                if (_nameOverride != value)
                {
                    _nameOverride = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this gallery item is actively loading the basemap or image.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
                }
            }
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
