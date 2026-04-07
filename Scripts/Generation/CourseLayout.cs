using System;
using System.Collections.Generic;
using Godot;

[Serializable]
public class CourseLayout
{
    public int Seed { get; set; }
    public int TotalHoles { get; set; } = 9;
    public Rect2 WorldBounds { get; set; } = new Rect2(new Vector2(-2400, -1800), new Vector2(4800, 3600));
    public List<HoleLayout> Holes { get; set; } = new List<HoleLayout>();

    public HoleLayout? GetHoleByNumber(int holeNumber)
    {
        for (var i = 0; i < Holes.Count; i += 1)
        {
            if (Holes[i].HoleNumber == holeNumber)
            {
                return Holes[i];
            }
        }

        return null;
    }
}
