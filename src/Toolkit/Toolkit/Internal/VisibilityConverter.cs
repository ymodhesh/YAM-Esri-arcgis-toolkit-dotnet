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
using System.Collections;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Globalization;
using System.Windows;
using System.Windows.Data;
#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// *FOR INTERNAL USE* Returns visible status for positive boolean, non-null text, enumerable with more than one
    /// value and opposite state for visibility value.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class VisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        object IValueConverter.Convert(object value, Type targetType, object parameter,
#if NETFX_CORE
            string language)
#else
            CultureInfo culture)
#endif
        {
            bool isVisible = false;

            if (value is bool)
            {
                isVisible = (bool)value;
            }
            else if (value is string)
            {
                isVisible = !string.IsNullOrWhiteSpace((string)value);
            }
            else if (value is IEnumerable enumerable)
            {
                int i = 0;
                foreach (object item in enumerable)
                { 
                    i++;
                    if (i > 1)
                    {
                        isVisible = true;
                        break;
                    }
                }
            }

            isVisible = parameter?.ToString() == "Reverse" ? !isVisible : isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter,
#if NETFX_CORE
            string language)
#else
            CultureInfo culture)
#endif
        {
            throw new NotSupportedException();
        }
    }
}
#endif