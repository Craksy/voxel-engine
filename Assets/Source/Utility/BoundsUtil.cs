using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility{
    public static class BoundsUtil
    {
        public static void ClampToRect(ref this Rect rect, Rect target){
            rect.xMin = Mathf.Clamp(rect.xMin, target.xMin, target.xMax);
            rect.yMin = Mathf.Clamp(rect.yMin, target.yMin, target.yMax);
            rect.xMax = Mathf.Clamp(rect.xMax, target.xMin, target.xMax);
            rect.yMax = Mathf.Clamp(rect.yMax, target.yMin, target.yMax);
        }
    }
}

