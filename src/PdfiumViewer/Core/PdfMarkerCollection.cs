using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PdfiumViewer.Core
{
    public class PdfMarkerCollection : ICollection<IPdfMarker>
    {
        private int _markerCount;
        private Dictionary<int, List<IPdfMarker>> _markers = new Dictionary<int, List<IPdfMarker>>();

        public int Count => _markerCount;

        public bool IsReadOnly => false;

        public void Add(IPdfMarker item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (_markers.TryGetValue(item.Page, out var markers))
                markers.Add(item);
            else
                _markers.Add(item.Page, new List<IPdfMarker> { item });
            _markerCount++;
        }
        public void AddRange(IEnumerable<IPdfMarker> items)
        {
            if(items == null) 
                throw new ArgumentNullException(nameof(items));
            foreach (var item in items)
                Add(item);
        }

        public void Clear()
        {
            _markers.Clear();
            _markerCount = 0;
        }

        public bool Contains(IPdfMarker item)
        {
            if (item == null)
                return false;
            if (_markers.TryGetValue(item.Page, out var markers))
                return markers.Contains(item);
            return false;
        }

        public IReadOnlyList<IPdfMarker> Get(int pageIndex)
        {
            if (_markers.TryGetValue(pageIndex, out var markers))
                return markers;
            return Array.Empty<IPdfMarker>();
        }

        public void CopyTo(IPdfMarker[] array, int arrayIndex)
        {
            foreach (var list in _markers.Values)
                foreach(var marker in list)
                    array[arrayIndex++] = marker;
        }

        public IEnumerator<IPdfMarker> GetEnumerator()
        {
            return _markers.Values.SelectMany(list => list).GetEnumerator();
        }

        public bool Remove(IPdfMarker item)
        {
            if (item == null)
                return false;
            if (_markers.TryGetValue(item.Page, out var markers) && markers.Remove(item))
            {
                _markerCount--;
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _markers.Values.SelectMany(list => list).GetEnumerator();
        }
    }
}
