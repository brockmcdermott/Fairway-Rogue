using Godot;

public static class DistanceCalculator
{
    public static float DistanceToCup(Vector2 fromPosition, Vector2 cupPosition)
    {
        return fromPosition.DistanceTo(cupPosition);
    }
}
