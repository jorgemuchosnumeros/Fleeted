using UnityEngine;

namespace Fleeted.utils;

public static class PathFinder
{
    public static string GetPath(this Transform current)
    {
        if (current.parent == null)
            return "/" + current.name;
        return current.parent.GetPath() + "/" + current.name;
    }

    public static string GetPath(this Component component)
    {
        return component.transform.GetPath() + "/" + component.GetType();
    }
}