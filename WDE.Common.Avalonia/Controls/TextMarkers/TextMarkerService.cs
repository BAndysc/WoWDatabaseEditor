// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace WDE.Common.Avalonia.Controls.TextMarkers
{
    /// <summary>
    /// Handles the text markers for a code editor.
    /// </summary>
    public sealed class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer, ITextMarkerService,
        ITextViewConnect
    {
        private readonly Dictionary<TextMarkerTypes, TextSegmentCollection<TextMarker>> markers;
        private readonly TextDocument document;

        public TextMarkerService(TextDocument document)
        {
            this.document = document;
            this.markers = new();
        }

        #region ITextMarkerService

        private TextSegmentCollection<TextMarker> GetMarkers(TextMarkerTypes type)
        {
            if (!markers.TryGetValue(type, out var list))
            {
                list = new TextSegmentCollection<TextMarker>(document);
                markers.Add(type, list);
            }

            return list;
        }
        
        public ITextMarker Create(TextMarkerTypes type, int startOffset, int length)
        {
            int textLength = document.TextLength;
            if (startOffset < 0 || startOffset > textLength)
                throw new ArgumentOutOfRangeException("startOffset", startOffset,
                    "Value must be between 0 and " + textLength);
            if (length < 0 || startOffset + length > textLength)
                throw new ArgumentOutOfRangeException("length", length,
                    "length must not be negative and startOffset+length must not be after the end of the document");

            TextMarker m = new TextMarker(this, type, startOffset, length);
            GetMarkers(type).Add(m);
            // no need to mark segment for redraw: the text marker is invisible until a property is set
            return m;
        }

        public IEnumerable<ITextMarker> GetMarkersAtOffset(TextMarkerTypes type, int offset)
        {
            return GetMarkers(type).FindSegmentsContaining(offset);
        }

        public void RemoveAll(TextMarkerTypes type)
        {
            if (markers.TryGetValue(type, out var collection))
                collection.Clear();
        }

        public void Remove(ITextMarker marker)
        {
            if (marker is TextMarker m && GetMarkers(marker.MarkerTypes).Remove(m))
            {
                Redraw(m);
                m.OnDeleted();
            }
        }

        /// <summary>
        /// Redraws the specified text segment.
        /// </summary>
        internal void Redraw(ISegment segment)
        {
            foreach (var view in textViews)
            {
                view.Redraw(segment.Offset, DispatcherPriority.Normal);
            }

            if (RedrawRequested != null)
                RedrawRequested(this, EventArgs.Empty);
        }

        public event EventHandler? RedrawRequested;

        #endregion

        #region DocumentColorizingTransformer

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStart = line.Offset;
            int lineEnd = lineStart + line.Length;
            foreach (var (type, collection) in markers)
            {
                foreach (TextMarker marker in collection.FindOverlappingSegments(lineStart, line.Length))
                {
                    IBrush? foregroundBrush = null;
                    if (marker.ForegroundBrush != null)
                    {
                        foregroundBrush = marker.ForegroundBrush;
                    }

                    ChangeLinePart(
                        Math.Max(marker.StartOffset, lineStart),
                        Math.Min(marker.EndOffset, lineEnd),
                        element =>
                        {
                            if (foregroundBrush != null)
                            {
                                element.TextRunProperties.SetForegroundBrush(foregroundBrush);
                            }

                            Typeface tf = element.TextRunProperties.Typeface;
                            element.TextRunProperties.SetTypeface((new Typeface(
                                tf.FontFamily,
                                marker.FontStyle ?? tf.Style,
                                marker.FontWeight ?? tf.Weight
                            )));
                        }
                    );
                }   
            }
        }

        #endregion

        #region IBackgroundRenderer

        public KnownLayer Layer
        {
            get
            {
                // draw behind selection
                return KnownLayer.Selection;
            }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!textView.VisualLinesValid)
                return;
            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;
            int viewStart = visualLines[0].FirstDocumentLine.Offset;
            int viewEnd = visualLines[^1].LastDocumentLine.EndOffset;
            foreach (var (type, collection) in markers)
            {
                foreach (TextMarker marker in collection.FindOverlappingSegments(viewStart, viewEnd - viewStart))
                {
                    if (marker.BackgroundBrush != null)
                    {
                        BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
                        geoBuilder.AlignToWholePixels = true;
                        geoBuilder.CornerRadius = 3;
                        geoBuilder.AddSegment(textView, marker);
                        Geometry geometry = geoBuilder.CreateGeometry();
                        if (geometry != null)
                        {
                            var brush = marker.BackgroundBrush;
                            drawingContext.DrawGeometry(brush, null, geometry);
                        }
                    }

                    var underlineMarkerTypes = TextMarkerTypes.SquigglyUnderline | TextMarkerTypes.NormalUnderline |
                                               TextMarkerTypes.DottedUnderline;
                    if ((marker.MarkerTypes & underlineMarkerTypes) != 0)
                    {
                        foreach (Rect r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                        {
                            Point startPoint = r.BottomLeft;
                            Point endPoint = r.BottomRight;

                            Brush usedBrush = new SolidColorBrush(marker.MarkerColor);
                            if ((marker.MarkerTypes & TextMarkerTypes.SquigglyUnderline) != 0)
                            {
                                double offset = 2.5;

                                int count = Math.Max((int)((endPoint.X - startPoint.X) / offset) + 1, 4);

                                StreamGeometry geometry = new StreamGeometry();

                                using (StreamGeometryContext ctx = geometry.Open())
                                {
                                    ctx.BeginFigure(startPoint, false);
                                    foreach (var p in CreatePoints(startPoint, endPoint, offset, count))
                                        ctx.LineTo(p);
                                    //ctx.PolyLineTo(CreatePoints(startPoint, endPoint, offset, count).ToArray(), true, false);
                                }

                                Pen usedPen = new Pen(usedBrush, 1);
                                drawingContext.DrawGeometry(Brushes.Transparent, usedPen, geometry);
                            }

                            if ((marker.MarkerTypes & TextMarkerTypes.NormalUnderline) != 0)
                            {
                                Pen usedPen = new Pen(usedBrush, 1);
                                drawingContext.DrawLine(usedPen, startPoint, endPoint);
                            }

                            if ((marker.MarkerTypes & TextMarkerTypes.DottedUnderline) != 0)
                            {
                                Pen usedPen = new Pen(usedBrush, 1, DashStyle.Dot);
                                drawingContext.DrawLine(usedPen, startPoint, endPoint);
                            }
                        }
                    }
                }
            }
        }

        IEnumerable<Point> CreatePoints(Point start, Point end, double offset, int count)
        {
            for (int i = 0; i < count; i++)
                yield return new Point(start.X + i * offset, start.Y - ((i + 1) % 2 == 0 ? offset : 0));
        }

        #endregion

        #region ITextViewConnect

        readonly List<TextView> textViews = new List<TextView>();

        void ITextViewConnect.AddToTextView(TextView textView)
        {
            if (!textViews.Contains(textView))
            {
                Debug.Assert(textView.Document == document);
                textViews.Add(textView);
            }
        }

        void ITextViewConnect.RemoveFromTextView(TextView textView)
        {
            Debug.Assert(textView.Document == document);
            textViews.Remove(textView);
        }

        #endregion
    }
}