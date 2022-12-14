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

#if !NETSTANDARD2_0
using Xamarin.Forms;
#if __ANDROID__
using Xamarin.Forms.Platform.Android;
#elif __IOS__
using Xamarin.Forms.Platform.iOS;
#elif NETFX_CORE
using Xamarin.Forms.Platform.UWP;
#endif

[assembly: ExportRenderer(typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.TimeSlider), typeof(Esri.ArcGISRuntime.Toolkit.Xamarin.Forms.TimeSliderRenderer))]

namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
{
    internal class TimeSliderRenderer : ViewRenderer<TimeSlider, Esri.ArcGISRuntime.Toolkit.UI.Controls.TimeSlider?>
    {
#if __ANDROID__
        public TimeSliderRenderer(Android.Content.Context context)
            : base(context)
        {
        }
#endif

        protected override void OnElementChanged(ElementChangedEventArgs<TimeSlider> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(e.NewElement?.NativeSlider);
            }
        }

#if !NETFX_CORE
        /// <inheritdoc />
        protected override bool ManageNativeControlLifetime => false;
#endif
    }
}
#endif