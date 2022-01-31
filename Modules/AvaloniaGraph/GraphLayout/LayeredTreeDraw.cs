// #define FIX
// using System;
// using System.Collections.Generic;
// using System.Linq;
//
// namespace AvaloniaGraph.GraphLayout;
//
// #region Enums
//
// public enum VerticalJustification
// {
//     top,
//     center,
//     bottom
// }
//
// #endregion
//
// public class LayeredTreeDraw : IDisposable
// {
//     #region Constructor
//
//     public LayeredTreeDraw(
//         ITreeNode tnRoot,
//         double pxBufferHorizontal,
//         double pxBufferHorizontalSubtree,
//         double pxBufferVertical,
//         VerticalJustification vj)
//     {
//         _pxBufferHorizontal = pxBufferHorizontal;
//         _pxBufferHorizontalSubtree = pxBufferHorizontalSubtree;
//         _pxBufferVertical = pxBufferVertical;
//         PxOverallHeight = 0.0;
//         _tnRoot = tnRoot;
//         _vj = vj;
//     }
//
//     #endregion
//
//     public void Dispose()
//     {
//         infos.Clear();
//     }
//
//     #region Internal classes
//
//     private class LayeredTreeInfo
//     {
//         public readonly List<double> lstPosLeftBoundaryRelativeToRoot = new();
//         public readonly List<double> lstPosRightBoundaryRelativeToRoot = new();
//
//         /// <summary>
//         ///     Initializes a new instance of the GraphLayoutInfo class.
//         /// </summary>
//         public LayeredTreeInfo(double subTreeWidth, ITreeNode tn)
//         {
//             SubTreeWidth = subTreeWidth;
//             pxLeftPosRelativeToParent = 0;
//             pxFromTop = 0;
//             ign = tn;
//         }
//
//         public double SubTreeWidth { get; }
//         public double pxLeftPosRelativeToParent { get; set; }
//         public double pxLeftPosRelativeToBoundingBox { get; set; }
//         public double pxToLeftSibling { get; set; }
//         public double pxFromTop { get; set; }
//         public double pxFromLeft { get; set; }
//         public ITreeNode ign { get; }
//     }
//
//     #endregion
//
//     #region Private variables
//
//     private readonly ITreeNode _tnRoot;
//     private readonly double _pxBufferHorizontal;
//     private readonly double _pxBufferHorizontalSubtree;
//     private readonly double _pxBufferVertical;
//     private readonly List<double> _lstLayerHeight = new();
//     private readonly VerticalJustification _vj;
//     private static readonly TreeNodeGroup _tngEmpty = new();
//
//     #endregion
//
//     #region Properties
//
//     public double PxOverallHeight { get; private set; }
//
//     public double PxOverallWidth => Info(_tnRoot)?.SubTreeWidth ?? 0;
//
//     public List<TreeConnection> Connections { get; } = new();
//
//     #endregion
//
//     #region PrivateInfo Access
//
//     private readonly Dictionary<ITreeNode, LayeredTreeInfo> infos = new();
//
//     private void SetInfo(ITreeNode node, LayeredTreeInfo info)
//     {
//         infos[node] = info;
//     }
//
//     private LayeredTreeInfo? Info(ITreeNode? ign)
//     {
//         if (ign == null)
//             return null;
//         if (infos.TryGetValue(ign, out var info))
//             return info;
//         return null;
//     }
//
//     public double X(ITreeNode tn)
//     {
//         var info = Info(tn);
//         if (info == null)
//             return 0;
//         return info.pxFromLeft;
//     }
//
//     public double Y(ITreeNode tn)
//     {
//         var info = Info(tn);
//         if (info == null) return 0;
//         return info.pxFromTop;
//     }
//
//     #endregion
//
//     #region Enumerations over nodes
//
//     public static IEnumerable<T> VisibleDescendants<T>(ITreeNode tn)
//     {
//         foreach (var tnCur in tn.TreeChildren)
//         {
//             if (!tnCur.Collapsed)
//                 foreach (var item in VisibleDescendants<T>(tnCur))
//                     yield return item;
//             yield return (T)tnCur;
//         }
//     }
//
//
//     public static IEnumerable<T> Descendants<T>(ITreeNode tn)
//     {
//         foreach (var tnCur in tn.TreeChildren)
//         {
//             foreach (var item in Descendants<T>(tnCur)) yield return item;
//             yield return (T)tnCur;
//         }
//     }
//
//     #endregion
//
//     #region Layout
//
//     #region Top Level Layout routines
//
//     public void LayoutTree()
//     {
//         LayoutTree(_tnRoot, 0);
//         DetermineFinalPositions(_tnRoot, 0, 0, Info(_tnRoot)?.pxLeftPosRelativeToBoundingBox ?? 0);
//     }
//
//     private void LayoutTree(ITreeNode tnRoot, int iLayer)
//     {
//         if (GetChildren(tnRoot).Count == 0)
//             LayoutLeafNode(tnRoot);
//         else
//             LayoutInteriorNode(tnRoot, iLayer);
//
//         UpdateLayerHeight(tnRoot, iLayer);
//     }
//
//     private void LayoutLeafNode(ITreeNode tnRoot)
//     {
//         var width = tnRoot.TreeWidth;
//         var lti = new LayeredTreeInfo(width, tnRoot);
//         lti.lstPosLeftBoundaryRelativeToRoot.Add(0);
//         lti.lstPosRightBoundaryRelativeToRoot.Add(width);
//         SetInfo(tnRoot, lti);
//     }
//
//     private void LayoutInteriorNode(ITreeNode tnRoot, int iLayer)
//     {
//         ITreeNode? tnLast = null;
//         var tng = GetChildren(tnRoot);
//         var itn = tng[0];
//         LayeredTreeInfo ltiThis;
//
//         LayoutAllOurChildren(iLayer, tnLast, tng);
//
//         // This width doesn't account for the parent node's width...
//         ltiThis = new LayeredTreeInfo(CalculateWidthFromInterChildDistances(tnRoot), tnRoot);
//         SetInfo(tnRoot, ltiThis);
//
//         // ...so that this centering may place the parent node negatively while the "width" is the width of
//         // all the child nodes.
//         CenterOverChildren(tnRoot, ltiThis);
//         DetermineParentRelativePositionsOfChildren(tnRoot);
//         CalculateBoundaryLists(tnRoot);
//     }
//
//     private void LayoutAllOurChildren(int iLayer, ITreeNode? tnLast, TreeNodeGroup tng)
//     {
//         var lstLeftToBB = new List<double>();
//         var lstResponsible = new List<int>();
//         for (var i = 0; i < tng.Count; i++)
//         {
//             var tn = tng[i];
//             LayoutTree(tn, iLayer + 1);
//             RepositionSubtree(i, tng, lstLeftToBB, lstResponsible);
//             tnLast = tn;
//         }
//     }
//
//     #endregion
//
//     #region Parent Relative Positioning
//
//     private void CenterOverChildren(ITreeNode tnRoot, LayeredTreeInfo ltiThis)
//     {
//         // We should be centered between  the connection points of our children...
//         var tnLeftMost = tnRoot.TreeChildren.LeftMost();
//         var pxLeftChild = Info(tnLeftMost)?.pxLeftPosRelativeToBoundingBox ?? 0 + tnLeftMost.TreeWidth / 2;
//         var tnRightMost = tnRoot.TreeChildren.RightMost();
//         var pxRightChild = Info(tnRightMost)?.pxLeftPosRelativeToBoundingBox ?? 0+ tnRightMost.TreeWidth / 2;
//         ltiThis.pxLeftPosRelativeToBoundingBox = (pxLeftChild + pxRightChild - tnRoot.TreeWidth) / 2;
//
//         // If the root node was wider than the subtree, then we'll have a negative position for it.  We need
//         // to readjust things so that the left of the root node represents the left of the bounding box and
//         // the child distances to the Bounding box need to be adjusted accordingly.
//         if (ltiThis.pxLeftPosRelativeToBoundingBox < 0)
//         {
//             foreach (var tnChildCur in tnRoot.TreeChildren)
//             {
//                 var info = Info(tnChildCur);
//                 if (info != null)
//                     info.pxLeftPosRelativeToBoundingBox -= ltiThis.pxLeftPosRelativeToBoundingBox;
//             }
//             ltiThis.pxLeftPosRelativeToBoundingBox = 0;
//         }
//     }
//
//     private void DetermineParentRelativePositionsOfChildren(ITreeNode tnRoot)
//     {
//         var ltiRoot = Info(tnRoot);
//         if (ltiRoot == null)
//             return;
//         
//         foreach (var tn in GetChildren(tnRoot))
//         {
//             var ltiCur = Info(tn);
//             if (ltiCur != null)
//                 ltiCur.pxLeftPosRelativeToParent =
//                     ltiCur.pxLeftPosRelativeToBoundingBox - ltiRoot.pxLeftPosRelativeToBoundingBox;
//         }
//     }
//
//     #endregion
//
//     #region Width Calculation
//
//     private double CalculateWidthFromInterChildDistances(ITreeNode tnRoot)
//     {
//         double pxWidthCur;
//         LayeredTreeInfo lti;
//         var pxWidth = 0.0;
//
//         lti = Info(tnRoot.TreeChildren.LeftMost())!;
//         pxWidthCur = lti.pxLeftPosRelativeToBoundingBox;
//
//         // If a subtree extends deeper than it's left neighbors then at that lower level it could potentially extend beyond those neighbors
//         // on the left.  We have to check for this and make adjustements after the loop if it occurred.
//         var pxUndercut = 0.0;
//
//         foreach (var tn in tnRoot.TreeChildren)
//         {
//             lti = Info(tn)!;
//             pxWidthCur += lti.pxToLeftSibling;
//
//             if (lti.pxLeftPosRelativeToBoundingBox > pxWidthCur)
//                 pxUndercut = Math.Max(pxUndercut, lti.pxLeftPosRelativeToBoundingBox - pxWidthCur);
//
//             // pxWidth might already be wider than the current node's subtree if earlier nodes "undercut" on the
//             // right hand side so we have to take the Max here...
//             pxWidth = Math.Max(pxWidth, pxWidthCur + lti.SubTreeWidth - lti.pxLeftPosRelativeToBoundingBox);
//
//             // After this next statement, the BoundingBox we're relative to is the one of our parent's subtree rather than
//             // our own subtree (with the exception of undercut considerations)
//             lti.pxLeftPosRelativeToBoundingBox = pxWidthCur;
//         }
//
//         if (pxUndercut > 0.0)
//         {
//             foreach (var tn in tnRoot.TreeChildren) Info(tn)!.pxLeftPosRelativeToBoundingBox += pxUndercut;
//             pxWidth += pxUndercut;
//         }
//
//         // We are never narrower than our root node's width which we haven't taken into account yet so
//         // we do that here.
//         return Math.Max(tnRoot.TreeWidth, pxWidth);
//     }
//
//     #endregion
//
//     #region Boundary Lists
//
//     private void CalculateBoundaryLists(ITreeNode tnRoot)
//     {
//         var lti = Info(tnRoot);
//         lti!.lstPosLeftBoundaryRelativeToRoot.Add(0.0);
//         lti.lstPosRightBoundaryRelativeToRoot.Add(tnRoot.TreeWidth);
//         DetermineBoundary(tnRoot.TreeChildren, true /* fLeft */, lti.lstPosLeftBoundaryRelativeToRoot);
//         DetermineBoundary(tnRoot.TreeChildren.Reverse(), false /* fLeft */, lti.lstPosRightBoundaryRelativeToRoot);
//     }
//
//     private void DetermineBoundary(IEnumerable<ITreeNode> entn, bool fLeft, List<double> lstPos)
//     {
//         var cLayersDeep = 1;
//         List<double> lstPosCur;
//         foreach (var tnChild in entn)
//         {
//             var ltiChild = Info(tnChild);
//
//             if (fLeft)
//                 lstPosCur = ltiChild!.lstPosLeftBoundaryRelativeToRoot;
//             else
//                 lstPosCur = ltiChild!.lstPosRightBoundaryRelativeToRoot;
//
//             if (lstPosCur.Count >= lstPos.Count)
//                 using (IEnumerator<double> enPosCur = lstPosCur.GetEnumerator())
//                 {
//                     for (var i = 0; i < cLayersDeep - 1; i++) enPosCur.MoveNext();
//
//                     while (enPosCur.MoveNext())
//                     {
//                         lstPos.Add(enPosCur.Current + ltiChild.pxLeftPosRelativeToParent);
//                         cLayersDeep++;
//                     }
//                 }
//         }
//     }
//
//     #endregion
//
//     #region Repositioning Children
//
//     private void ApportionSlop(int itn, int itnResponsible, TreeNodeGroup tngSiblings)
//     {
//         var lti = Info(tngSiblings[itn]);
//         var tnLeft = tngSiblings[itn - 1];
//
//         var pxSlop = lti!.pxToLeftSibling - tnLeft.TreeWidth - _pxBufferHorizontal;
//         if (pxSlop > 0)
//         {
//             for (var i = itnResponsible + 1; i < itn; i++)
//                 Info(tngSiblings[i])!.pxToLeftSibling += pxSlop * (i - itnResponsible) / (itn - itnResponsible);
//             lti.pxToLeftSibling -= (itn - itnResponsible - 1) * pxSlop / (itn - itnResponsible);
//         }
//     }
//
//     private void RepositionSubtree(
//         int itn,
//         TreeNodeGroup tngSiblings,
//         List<double> lstLeftToBB,
//         List<int> lsttnResponsible)
//     {
//         int itnResponsible;
//         var tn = tngSiblings[itn];
//         var lti = Info(tn);
//
//         if (itn == 0)
//         {
//             // No shifting but we still have to prepare the initial version of the
//             // left hand skeleton list
//             foreach (var pxRelativeToRoot in lti!.lstPosRightBoundaryRelativeToRoot)
//             {
//                 lstLeftToBB.Add(pxRelativeToRoot + lti.pxLeftPosRelativeToBoundingBox);
//                 lsttnResponsible.Add(0);
//             }
//
//             return;
//         }
//
//         var tnLeft = tngSiblings[itn - 1];
//         var ltiLeft = Info(tnLeft);
//         int iLayer;
//         var pxHorizontalBuffer = _pxBufferHorizontal;
//
//         var pxNewPosFromBB = PxCalculateNewPos(lti!, lstLeftToBB, lsttnResponsible, out itnResponsible, out iLayer);
//         if (iLayer != 0) pxHorizontalBuffer = _pxBufferHorizontalSubtree;
//
//         lti!.pxToLeftSibling = pxNewPosFromBB - lstLeftToBB.First() + tnLeft.TreeWidth + pxHorizontalBuffer;
//
//         var cLevels = Math.Min(lti.lstPosRightBoundaryRelativeToRoot.Count, lstLeftToBB.Count);
//         for (var i = 0; i < cLevels; i++)
//         {
//             lstLeftToBB[i] = lti.lstPosRightBoundaryRelativeToRoot[i] + pxNewPosFromBB + pxHorizontalBuffer;
//             lsttnResponsible[i] = itn;
//         }
//
//         for (var i = lstLeftToBB.Count; i < lti.lstPosRightBoundaryRelativeToRoot.Count; i++)
//         {
//             lstLeftToBB.Add(lti.lstPosRightBoundaryRelativeToRoot[i] + pxNewPosFromBB + pxHorizontalBuffer);
//             lsttnResponsible.Add(itn);
//         }
//
//         ApportionSlop(itn, itnResponsible, tngSiblings);
//     }
//
//     private double PxCalculateNewPos(
//         LayeredTreeInfo lti,
//         List<double> lstLeftToBB,
//         List<int> lstitnResponsible,
//         out int itnResponsible,
//         out int iLayerRet)
//     {
//         var pxOffsetToBB = lstLeftToBB[0];
//         var cLayers = Math.Min(lti.lstPosLeftBoundaryRelativeToRoot.Count, lstLeftToBB.Count);
//         var pxRootPosRightmost = 0.0;
//         iLayerRet = 0;
//
//         using (IEnumerator<double> enRight = lti.lstPosLeftBoundaryRelativeToRoot.GetEnumerator(),
//                enLeft = lstLeftToBB.GetEnumerator())
//         using (IEnumerator<int> enResponsible = lstitnResponsible.GetEnumerator())
//         {
//             itnResponsible = -1;
//
//             enRight.MoveNext();
//             enLeft.MoveNext();
//             enResponsible.MoveNext();
//             for (var iLayer = 0; iLayer < cLayers; iLayer++)
//             {
//                 var pxLeftBorderFromBB = enLeft.Current;
//                 var pxRightBorderFromRoot = enRight.Current;
//                 double pxRightRootBasedOnThisLevel;
//                 var itnResponsibleCur = enResponsible.Current;
//
//                 enLeft.MoveNext();
//                 enRight.MoveNext();
//                 enResponsible.MoveNext();
//
//                 pxRightRootBasedOnThisLevel = pxLeftBorderFromBB - pxRightBorderFromRoot;
//                 if (pxRightRootBasedOnThisLevel > pxRootPosRightmost)
//                 {
//                     iLayerRet = iLayer;
//                     pxRootPosRightmost = pxRightRootBasedOnThisLevel;
//                     itnResponsible = itnResponsibleCur;
//                 }
//             }
//         }
//
//         return pxRootPosRightmost;
//     }
//
//     #endregion
//
//     #region Height Calculations
//
//     private void UpdateLayerHeight(ITreeNode tnRoot, int iLayer)
//     {
//         while (_lstLayerHeight.Count <= iLayer) _lstLayerHeight.Add(0.0);
//         _lstLayerHeight[iLayer] = Math.Max(tnRoot.TreeHeight, _lstLayerHeight[iLayer]);
//     }
//
//     private double CalcJustify(double height, double pxRowHeight)
//     {
//         var dRet = 0.0;
//
//         switch (_vj)
//         {
//             case VerticalJustification.top:
//                 break;
//
//             case VerticalJustification.center:
//                 dRet = (pxRowHeight - height) / 2;
//                 break;
//
//             case VerticalJustification.bottom:
//                 dRet = pxRowHeight - height;
//                 break;
//         }
//
//         return dRet;
//     }
//
//     #endregion
//
//     #region Collapse handling
//
//     private TreeNodeGroup GetChildren(ITreeNode tn)
//     {
//         if (tn.Collapsed) return _tngEmpty;
//         return tn.TreeChildren;
//     }
//
//     #endregion
//
//     #region Second pass to convert parent relative positions to absolute positions
//
//     private void DetermineFinalPositions(ITreeNode tn, int iLayer, double pxFromTop, double pxParentFromLeft)
//     {
//         var pxRowHeight = _lstLayerHeight[iLayer];
//         var lti = Info(tn);
//         double pxBottom;
//         DPoint dptOrigin;
//
//         lti!.pxFromTop = pxFromTop + CalcJustify(tn.TreeHeight, pxRowHeight);
//         pxBottom = lti.pxFromTop + tn.TreeHeight;
//         if (pxBottom > PxOverallHeight) PxOverallHeight = pxBottom;
//         lti.pxFromLeft = lti.pxLeftPosRelativeToParent + pxParentFromLeft;
//         dptOrigin = new DPoint(lti.pxFromLeft + tn.TreeWidth / 2, lti.pxFromTop + tn.TreeHeight);
//         iLayer++;
//         foreach (var tnCur in GetChildren(tn))
//         {
//             var lstcpt = new List<DPoint>();
//             var ltiCur = Info(tnCur);
//             lstcpt.Add(dptOrigin);
//             DetermineFinalPositions(tnCur, iLayer, pxFromTop + pxRowHeight + _pxBufferVertical, lti.pxFromLeft);
//             lstcpt.Add(new DPoint(ltiCur!.pxFromLeft + tnCur.TreeWidth / 2, ltiCur.pxFromTop));
//             Connections.Add(new TreeConnection(tn, tnCur, lstcpt));
//         }
//     }
//
//     #endregion
//
//     #endregion
// }