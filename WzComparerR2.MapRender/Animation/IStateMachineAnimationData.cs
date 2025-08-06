using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Animation
{
    public interface IStateMachineAnimationData
    {
        ReadOnlyCollection<string> States { get; }
        int SelectedStateIndex { get; set; }
        string SelectedState { get; }
        Point CurrentLT { get; }
        Point CurrentRB { get; }
        void Update(TimeSpan elapsedTime);
        event EventHandler AnimationEnd;
        object GetMesh();
    }
}
