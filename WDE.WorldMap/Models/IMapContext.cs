using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WDE.WorldMap.Models
{
    public interface IMapContext
    {
        public void Center(double x, double y);
        public event Action RequestRender;
        public event Action<double, double> RequestCenter;
        public event Action<double, double, double, double> RequestBoundsToView;
        void Initialized();
    }
    
    public interface IMapContext<T> : IMapContext where T : IMapItem
    {
        public IEnumerable<T> VisibleItems { get; }
        public T? SelectedItem { get; set; }

        public void Move(T item, double x, double y);
        public void StartMove() { }
        public void StopMove() { }
    }
}