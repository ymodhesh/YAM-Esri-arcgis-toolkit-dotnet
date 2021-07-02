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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    /// <summary>
    /// Modifiable, observable collection with pinning. A single <see cref="PinnedItem"/> will remain at the top of the list.
    /// </summary>
    public class PinnableCollection<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged, IList
        where T : class
    {
        private T? _pinnedItem;
        private readonly List<T> _unpinnedItems;
        private readonly List<T> _fullView;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinnableCollection{T}"/> class.
        /// </summary>
        public PinnableCollection()
        {
            _unpinnedItems = new List<T>();
            _fullView = new List<T>();
        }

        /// <summary>
        /// Gets or sets the pinned item. The pinned item stays at the top (0 position) of the collection.
        /// Pinned item is immune to <see cref="Remove(T)"/>, <see cref="RemoveAt(int)"/>, <see cref="Clear"/>, and other collection change operations.
        /// </summary>
        public T? PinnedItem
        {
            get => _pinnedItem;
            set
            {
                if (_pinnedItem != value)
                {
                    var oldValue = _pinnedItem;
                    _pinnedItem = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PinnedItem)));
                    UpdateFullView();

                    if (oldValue != null && _pinnedItem == null)
                    {
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _pinnedItem, 0));
                    }
                    else if (oldValue == null && _pinnedItem != null)
                    {
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _pinnedItem, 0));
                    }
                    else if (oldValue != null && _pinnedItem != null)
                    {
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, _pinnedItem, oldValue));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the element at the index. If set, <see cref="PinnedItem"/> is at position 0. Setting values by index is not supported.
        /// </summary>
        public T this[int index]
        {
            get => _fullView[index];
            set => throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        int ICollection<T>.Count => _fullView.Count;

        int ICollection.Count => _fullView.Count;

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        bool ICollection<T>.IsReadOnly => false;

        object? IList.this[int index]
        {
            get => _fullView[index];
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the position of the specified item. If set, <see cref="PinnedItem"/> is at position 0.
        /// </summary>
        public int IndexOf(T item) => _fullView.IndexOf(item);

        /// <summary>
        /// Inserts the item at the specified index. Index starts with the first unpinned item at position 0.
        /// </summary>
        public void Insert(int index, T item)
        {
            _unpinnedItems.Insert(index, item);
            UpdateFullView();
            var newIndex = _fullView.IndexOf(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, newIndex));
        }

        /// <summary>
        /// Removes the item at the specified index. Note: index starts at 0 for unpinned items.
        /// </summary>
        public void RemoveAt(int index)
        {
            var item = _unpinnedItems.ElementAt(index);
            var publicIndex = _fullView.IndexOf(item);
            _unpinnedItems.Remove(item);
            UpdateFullView();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, publicIndex));
        }

        /// <summary>
        /// Adds the item to this collection.
        /// </summary>
        public void Add(T item)
        {
            _unpinnedItems.Add(item);
            UpdateFullView();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _fullView.IndexOf(item)));
        }

        /// <summary>
        /// Adds the items to this collection.
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                _unpinnedItems.Add(item);
            }

            UpdateFullView();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        internal bool UpdatingCollectionFlag = false;

        /// <summary>
        /// Clears all items, except for <see cref="PinnedItem"/>.
        /// </summary>
        public void Clear()
        {
            // working around weird collection behavior on Forms UWP
            UpdatingCollectionFlag = true;
            _unpinnedItems.Clear();
            UpdateFullView();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            UpdatingCollectionFlag = false;
        }

        /// <summary>
        /// Returns true if the collection (excluding <see cref="PinnedItem"/>) contains the specified item.
        /// </summary>
        public bool Contains(T item) => _fullView?.Contains(item) ?? false;

        /// <summary>
        /// Returns true if the specified item is in the collection. Ignores the pinned item unless <paramref name="includePinned"/> is true.
        /// </summary>
        public bool Contains(T item, bool includePinned)
        {
            if (includePinned)
            {
                return Contains(item);
            }
            else
            {
                return _unpinnedItems.Contains(item);
            }
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _fullView?.CopyTo(array, arrayIndex);

        /// <summary>
        /// Removes the specified item. Item must not be <see cref="PinnedItem"/>.
        /// </summary>
        public bool Remove(T item)
        {
            var publicIndex = _fullView.IndexOf(item);
            _unpinnedItems.Remove(item);
            UpdateFullView();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, publicIndex));
            return true;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _fullView.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _fullView.GetEnumerator();

        int IList.Add(object? value)
        {
            if (value is T item)
            {
                Add(item);
                return IndexOf(item);
            }

            return -1;
        }

        bool IList.Contains(object? value)
        {
            if (value is T item)
            {
                return Contains(item);
            }

            return false;
        }

        int IList.IndexOf(object? value)
        {
            if (value is T item)
            {
                return IndexOf(item);
            }

            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            if (value is T item)
            {
                Insert(index, item);
            }
        }

        void IList.Remove(object? value)
        {
            if (value is T item)
            {
                Remove(item);
            }
        }

        void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();

        void IList.Clear() => Clear();

        void IList.RemoveAt(int index) => RemoveAt(index);

        private void UpdateFullView()
        {
            _fullView.Clear();
            if (_pinnedItem != null)
            {
                _fullView.Add(_pinnedItem);
            }

            _fullView.AddRange(_unpinnedItems);
        }
    }
}
