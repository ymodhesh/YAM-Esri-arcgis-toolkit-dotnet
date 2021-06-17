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
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls.BasemapGallery
{
    /// <summary>
    /// Encompasses an element in a basemap gallery.
    /// </summary>
    public class BasemapGalleryItem
    {
        private readonly RuntimeImage _thumbnailOverride;
        private readonly string _tooltipOverride;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGalleryItem"/> class.
        /// </summary>
        /// <param name="basemap">Basemap for this gallery item. Must be not null and loaded.</param>
        public BasemapGalleryItem(Basemap basemap)
        {
            if (basemap?.LoadStatus != LoadStatus.Loaded)
            {
                throw new ArgumentException("Basemap must be non-null and loaded (LoadStatus == Loaded)");
            }

            Basemap = basemap;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasemapGalleryItem"/> class.
        /// </summary>
        /// <param name="basemap">Basemap for this gallery item. Must be not null and loaded.</param>
        /// <param name="thumbnail">Thumbnail to display for this gallery item. If not null, will take precedence over the basemap item's thumbnail.</param>
        /// <param name="tooltip">Tooltip to display for this gallery item. If not null, will take precdence of the basemap item's description.</param>
        public BasemapGalleryItem(Basemap basemap, RuntimeImage thumbnail, string tooltip)
            : this(basemap)
        {
            _thumbnailOverride = thumbnail;
            _tooltipOverride = tooltip;
        }

        /// <summary>
        /// Gets the basemap associated with this basemap item.
        /// </summary>
        public Basemap Basemap { get; private set; }

        /// <summary>
        /// Gets the thumbnail to display for this basemap item.
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
        }

        /// <summary>
        /// Gets the tooltip to display for this basemap item.
        /// </summary>
        public string Tooltip
        {
            get
            {
                if (_tooltipOverride != null)
                {
                    return _tooltipOverride;
                }

                return Basemap.Item.Description;
            }
        }

        /// <summary>
        /// Gets the name to display for this basemap item.
        /// </summary>
        public string Name
        {
            get => Basemap.Name;
        }
    }
}
