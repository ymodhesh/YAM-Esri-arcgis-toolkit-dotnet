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
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Collections.ObjectModel;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#endif

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    internal class TraceConfigurationsViewDataSource : IList<UtilityNamedTraceConfiguration>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private GeoView _geoView;
        private IList<UtilityNamedTraceConfiguration> _overrideList;

        private IList<UtilityNamedTraceConfiguration> ActiveTraceConfigurationList
        {
            get
            {
                if (_overrideList != null)
                {
                    return _overrideList;
                }
                //else if (_geoView is MapView mv && mv.Map?.TraceConfigurations != null)
                //{
                //    return mv.Map.TraceConfigurations;
                //}
                //else if (_geoView is SceneView sv && sv.Scene?.TraceConfigurations != null)
                //{
                //    return sv.Scene?.TraceConfigurations;
                //}

                return new UtilityNamedTraceConfiguration[] { };
            }
        }

        /// <summary>
        /// Sets the override TraceConfiguration list that will be shown instead of the Map's TraceConfiguration list.
        /// </summary>
        /// <param name="TraceConfigurations">List of TraceConfigurations to show.</param>
        public void SetOverrideList(IEnumerable<UtilityNamedTraceConfiguration> TraceConfigurations)
        {
            // Skip if collection is the same
            if (_overrideList == TraceConfigurations)
            {
                return;
            }

            // Set new list
            if (TraceConfigurations == null)
            {
                _overrideList = null;
            }
            else if (TraceConfigurations is IList<UtilityNamedTraceConfiguration> listOfTraceConfigurations)
            {
                _overrideList = listOfTraceConfigurations;
            }
            else
            {
                _overrideList = TraceConfigurations.ToList();
            }

            // Refresh
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            // Subscribe to events if applicable
            if (TraceConfigurations is INotifyCollectionChanged iCollectionChanged)
            {
                var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(iCollectionChanged);
                listener.OnEventAction = (instance, source, eventArgs) => HandleOverrideListCollectionChanged(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
                iCollectionChanged.CollectionChanged += listener.OnEvent;
            }
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        /// <summary>
        /// Sets the GeoView from which TraceConfigurations will be shown.
        /// </summary>
        /// <param name="view">The view from which to get Map/Scene TraceConfigurations.</param>
        public void SetGeoView(GeoView view)
        {
            if (_geoView == view)
            {
                return;
            }

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    mapview.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoView is SceneView sceneview)
                {
#if NETFX_CORE
                    sceneview.UnregisterPropertyChangedCallback(SceneView.SceneProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).RemoveValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#else
                (_geoView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
#endif
            }

            _geoView = view;

            if (_overrideList == null)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = mapview.RegisterPropertyChangedCallback(MapView.MapProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
                else if (_geoView is SceneView sceneview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = sceneview.RegisterPropertyChangedCallback(SceneView.SceneProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(SceneView.SceneProperty, typeof(SceneView)).AddValueChanged(sceneview, GeoViewDocumentChanged);
#endif
                }
#else

                (_geoView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#endif

                // Handle case where geoview loads map while events are being set up
                GeoViewDocumentChanged(null, null);
            }
        }

#if XAMARIN || XAMARIN_FORMS
        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                GeoViewDocumentChanged(sender, e);
            }
        }
#endif

        private void GeoViewDocumentChanged(object sender, object e)
        {
            if (_geoView is MapView mv && mv.Map is ILoadable mapLoadable)
            {
                // Listen for load completion
                var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(mapLoadable);
                listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                mapLoadable.Loaded += listener.OnEvent;

                // Ensure event is raised even if already loaded
                _ = mv.Map.RetryLoadAsync();
            }
            else if (_geoView is SceneView sv && sv.Scene is ILoadable sceneLoadable)
            {
                // Listen for load completion
                var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(sceneLoadable);
                listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                sceneLoadable.Loaded += listener.OnEvent;

                // Ensure event is raised even if already loaded
                _ = sv.Scene.RetryLoadAsync();
            }

            if (_overrideList == null)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void Doc_Loaded(object sender, EventArgs e)
        {
            // Get new TraceConfigurations collection
            ObservableCollection<UtilityNamedTraceConfiguration> bmCollection = new ObservableCollection<UtilityNamedTraceConfiguration>();
            //if (sender is Map map)
            //{
            //    bmCollection = map.TraceConfigurations;
            //}
            //else if (sender is Scene scene)
            //{
            //    bmCollection = scene.TraceConfigurations;
            //}
            //else
            //{
            //    return;
            //}

            // Update list
            if (_overrideList == null)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(bmCollection);
            listener.OnEventAction = (instance, source, eventArgs) => HandleGeoViewTraceConfigurationsCollectionChanged(source, eventArgs);
            listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
            bmCollection.CollectionChanged += listener.OnEvent;
        }

        private void HandleGeoViewTraceConfigurationsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Don't do anything if the override list is there
            if (_overrideList != null)
            {
                return;
            }

            OnCollectionChanged(e);
        }

        private void HandleOverrideListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_overrideList != null)
            {
                OnCollectionChanged(e);
            }
        }

        private void RunOnUIThread(Action action)
        {
#if XAMARIN_FORMS
            global::Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
#elif __IOS__
            _geoView.InvokeOnMainThread(action);
#elif __ANDROID__
            _geoView.PostDelayed(action, 500);
#elif NETFX_CORE
            _ = _geoView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => action());
#else
            _geoView.Dispatcher.Invoke(action);
#endif
        }

        #region IList<UtilityNamedTraceConfiguration> implementation
        UtilityNamedTraceConfiguration IList<UtilityNamedTraceConfiguration>.this[int index] { get => ActiveTraceConfigurationList?[index]; set => throw new NotImplementedException(); }

        int ICollection<UtilityNamedTraceConfiguration>.Count => ActiveTraceConfigurationList?.Count ?? 0;

        bool ICollection<UtilityNamedTraceConfiguration>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        int ICollection.Count => ActiveTraceConfigurationList?.Count ?? 0;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        object IList.this[int index] { get => ActiveTraceConfigurationList?[index]; set => throw new NotImplementedException(); }

        void ICollection<UtilityNamedTraceConfiguration>.Add(UtilityNamedTraceConfiguration item) => throw new NotImplementedException();

        void ICollection<UtilityNamedTraceConfiguration>.Clear() => throw new NotImplementedException();

        bool ICollection<UtilityNamedTraceConfiguration>.Contains(UtilityNamedTraceConfiguration item) => ActiveTraceConfigurationList?.Contains(item) ?? false;

        void ICollection<UtilityNamedTraceConfiguration>.CopyTo(UtilityNamedTraceConfiguration[] array, int arrayIndex) => ActiveTraceConfigurationList?.CopyTo(array, arrayIndex);

        IEnumerator<UtilityNamedTraceConfiguration> IEnumerable<UtilityNamedTraceConfiguration>.GetEnumerator() => ActiveTraceConfigurationList?.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ActiveTraceConfigurationList?.GetEnumerator();

        int IList<UtilityNamedTraceConfiguration>.IndexOf(UtilityNamedTraceConfiguration item) => ActiveTraceConfigurationList?.IndexOf(item) ?? -1;

        void IList<UtilityNamedTraceConfiguration>.Insert(int index, UtilityNamedTraceConfiguration item) => throw new NotImplementedException();

        bool ICollection<UtilityNamedTraceConfiguration>.Remove(UtilityNamedTraceConfiguration item) => throw new NotImplementedException();

        void IList<UtilityNamedTraceConfiguration>.RemoveAt(int index) => throw new NotImplementedException();

        int IList.Add(object value) => throw new NotImplementedException();

        bool IList.Contains(object value) => ActiveTraceConfigurationList?.Contains(value) ?? false;

        void IList.Clear() => throw new NotImplementedException();

        int IList.IndexOf(object value) => ActiveTraceConfigurationList?.IndexOf(value as UtilityNamedTraceConfiguration) ?? -1;

        void IList.Insert(int index, object value) => throw new NotImplementedException();

        void IList.Remove(object value) => throw new NotImplementedException();

        void IList.RemoveAt(int index) => throw new NotImplementedException();

        void ICollection.CopyTo(Array array, int index) => (ActiveTraceConfigurationList as ICollection)?.CopyTo(array, index);
        #endregion IList<UtilityNamedTraceConfiguration> implementation

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            RunOnUIThread(() =>
            {
                CollectionChanged?.Invoke(this, args);
                OnPropertyChanged("Item[]");
                if (args.Action != NotifyCollectionChangedAction.Move)
                {
                    OnPropertyChanged(nameof(IList.Count));
                }
            });
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
