using System;
using WzComparerR2.Common;

namespace WzComparerR2.Animation
{
    public interface ISpineAnimationData
    {
        bool PremultipliedAlpha { get; }
        object SkeletonData { get; }
        object Atlas { get; }
        SpineVersion SpineVersion { get; }
        ISpineAnimator CreateAnimator();
    }
}
