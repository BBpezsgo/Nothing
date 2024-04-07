// KDTree.cs - A Stark, September 2009.

//	This class implements a data structure that stores a list of points in space.
//	A common task in game programming is to take a supplied point and discover which
//	of a stored set of points is nearest to it. For example, in path-plotting, it is often
//	useful to know which waypoint is nearest to the player's current
//	position. The kd-tree allows this "nearest neighbor" search to be carried out quickly,
//	or at least much more quickly than a simple linear search through the list.

//	At present, the class only allows for construction (using the MakeFromPoints static method)
//	and nearest-neighbor searching (using FindNearest). More exotic kd-trees are possible, and
//	this class may be extended in the future if there seems to be a need.

//	The nearest-neighbor search returns an integer index - it is assumed that the original
//	array of points is available for the lifetime of the tree, and the index refers to that
//	array.

using System.Text;
using UnityEngine;

#nullable enable

public class KDTree
{

    public KDTree[] lr;
    public Vector3 pivot;
    public int pivotIndex;
    public int axis;

    /// <summary>
    /// Change this value to 2 if you only need two-dimensional X,Y points.
    /// The search will be quicker in two dimensions.
    /// </summary>
    const int numDims = 3;

    public KDTree()
    {
        lr = new KDTree[2];
    }

    /// <summary>
    /// Make a new tree from a list of points.
    /// </summary>
    public static KDTree MakeFromPoints(params Vector3[] points)
    {
        int[] indices = Iota(points.Length);
        return MakeFromPointsInner(0, 0, points.Length - 1, points, indices);
    }

    /// <summary>
    /// Recursively build a tree by separating points at plane boundaries.
    /// </summary>
    static KDTree MakeFromPointsInner(int depth, int startIndex, int endIndex, Vector3[] points, int[] indices)
    {
        KDTree root = new()
        { axis = depth % numDims };

        int splitPoint = FindPivotIndex(points, indices, startIndex, endIndex, root.axis);

        root.pivotIndex = indices[splitPoint];
        root.pivot = points[root.pivotIndex];

        int leftEndIndex = splitPoint - 1;

        if (leftEndIndex >= startIndex)
        { root.lr[0] = MakeFromPointsInner(depth + 1, startIndex, leftEndIndex, points, indices); }

        int rightStartIndex = splitPoint + 1;

        if (rightStartIndex <= endIndex)
        { root.lr[1] = MakeFromPointsInner(depth + 1, rightStartIndex, endIndex, points, indices); }

        return root;
    }

    static void SwapElements(int[] array, int indexA, int indexB)
    {
        int temp = array[indexA];
        array[indexA] = array[indexB];
        array[indexB] = temp;
    }

    /// <summary>
    /// Simple "median of three" heuristic to find a reasonable splitting plane.
    /// </summary>
    static int FindSplitPoint(Vector3[] points, int[] indices, int startIndex, int endIndex, int axis)
    {
        float a = points[indices[startIndex]][axis];
        float b = points[indices[endIndex]][axis];
        int midIndex = (startIndex + endIndex) / 2;
        float m = points[indices[midIndex]][axis];

        if (a > b)
        {
            if (m > a)
            { return startIndex; }

            if (b > m)
            { return endIndex; }

            return midIndex;
        }
        else
        {
            if (a > m)
            { return startIndex; }

            if (m > b)
            { return endIndex; }

            return midIndex;
        }
    }

    /// <summary>
    /// Find a new pivot index from the range by splitting the points that fall either side
    /// of its plane.
    /// </summary>
    public static int FindPivotIndex(Vector3[] points, int[] indices, int startIndex, int endIndex, int axis)
    {
        int splitPoint = FindSplitPoint(points, indices, startIndex, endIndex, axis);
        // int splitPoint = Random.Range(stIndex, enIndex);

        Vector3 pivot = points[indices[splitPoint]];
        SwapElements(indices, startIndex, splitPoint);

        int currPt = startIndex + 1;
        int endPt = endIndex;

        while (currPt <= endPt)
        {
            Vector3 curr = points[indices[currPt]];

            if ((curr[axis] > pivot[axis]))
            {
                SwapElements(indices, currPt, endPt);
                endPt--;
            }
            else
            {
                SwapElements(indices, currPt - 1, currPt);
                currPt++;
            }
        }

        return currPt - 1;
    }

    public static int[] Iota(int num)
    {
        int[] result = new int[num];

        for (int i = 0; i < num; i++)
        { result[i] = i; }

        return result;
    }

    /// <summary>
    /// Find the nearest point in the set to the supplied point.
    /// </summary>
    public (int Index, float SqrDistance) FindNearest(Vector3 point)
    {
        float bestSqDist = float.MaxValue;
        int bestIndex = -1;

        Search(point, ref bestSqDist, ref bestIndex);

        return (bestIndex, bestSqDist);
    }

    /// <summary>
    /// Recursively search the tree.
    /// </summary>
    void Search(Vector3 point, ref float bestSqrDistSoFar, ref int bestIndex)
    {
        float mySqDist = (pivot - point).sqrMagnitude;

        if (mySqDist < bestSqrDistSoFar)
        {
            bestSqrDistSoFar = mySqDist;
            bestIndex = pivotIndex;
        }

        float planeDist = point[axis] - pivot[axis]; // DistFromSplitPlane(pt, pivot, axis);

        int selector = planeDist <= 0 ? 0 : 1;

        lr[selector]?.Search(point, ref bestSqrDistSoFar, ref bestIndex);

        selector = (selector + 1) % 2;

        float sqPlaneDist = planeDist * planeDist;

        if ((lr[selector] != null) && (bestSqrDistSoFar > sqPlaneDist))
        {
            lr[selector].Search(point, ref bestSqrDistSoFar, ref bestIndex);
        }
    }

    /// <summary>
    /// Get a point's distance from an axis-aligned plane.
    /// </summary>
    float DistFromSplitPlane(Vector3 pt, Vector3 planePt, int axis)
    {
        return pt[axis] - planePt[axis];
    }

    /// <summary>
    /// Simple output of tree structure - mainly useful for getting a rough
    /// idea of how deep the tree is (and therefore how well the splitting
    /// heuristic is performing).
    /// </summary>
    public string Dump(int level)
    {
        StringBuilder builder = new();
        Dump(level, builder);
        return builder.ToString();
    }

    /// <summary>
    /// Simple output of tree structure - mainly useful for getting a rough
    /// idea of how deep the tree is (and therefore how well the splitting
    /// heuristic is performing).
    /// </summary>
    public void Dump(int level, StringBuilder builder)
    {
        builder.Append(pivotIndex.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(level, ' '));
        builder.Append('\n');

        lr[0]?.Dump(level + 2, builder);
        lr[1]?.Dump(level + 2, builder);
    }
}
