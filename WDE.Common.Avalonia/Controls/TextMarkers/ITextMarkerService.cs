using System.Collections.Generic;

namespace WDE.Common.Avalonia.Controls.TextMarkers;

public interface ITextMarkerService
{
    /// <summary>
    /// Creates a new text marker. The text marker will be invisible at first,
    /// you need to set one of the Color properties to make it visible.
    /// </summary>
    ITextMarker Create(TextMarkerTypes type, int startOffset, int length);
		
    /// <summary>
    /// Removes the specified text marker.
    /// </summary>
    void Remove(ITextMarker marker);
		
    /// <summary>
    /// Removes all text markers that match the condition.
    /// </summary>
    void RemoveAll(TextMarkerTypes type);
		
    /// <summary>
    /// Finds all text markers at the specified offset.
    /// </summary>
    IEnumerable<ITextMarker> GetMarkersAtOffset(TextMarkerTypes type, int offset);
}