using Godot;

public static class AutoloadLocator
{
    public static T? Get<T>(Node context, string autoloadName) where T : class
    {
        var tree = context.GetTree();
        var root = tree?.Root;
        if (root == null)
        {
            return null;
        }

        return root.GetNodeOrNull<T>(autoloadName);
    }
}
