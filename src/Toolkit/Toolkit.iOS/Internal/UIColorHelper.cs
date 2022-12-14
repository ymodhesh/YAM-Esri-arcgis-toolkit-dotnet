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
using System.Text;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit
{
    internal static class UIColorHelper
    {
        private static readonly bool _isIOS13OrNewer;

        static UIColorHelper()
        {
            _isIOS13OrNewer = UIDevice.CurrentDevice.CheckSystemVersion(13, 0);
        }

        public static UIColor LabelColor => _isIOS13OrNewer ? UIColor.LabelColor : UIColor.Black;

        public static UIColor SystemBackgroundColor => _isIOS13OrNewer ? UIColor.SystemBackgroundColor : UIColor.White;
    }
}
