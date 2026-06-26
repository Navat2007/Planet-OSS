using Planet.Sim;
using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Преобразования между единицами симуляции (целые мм, плоскость XZ) и миром Unity (метры).</summary>
    public static class SimConvert
    {
        public static Vector3 ToWorld(SimVector2 p, float y = 0f)
        {
            float scale = SimConstants.UnitsPerMeter;
            return new Vector3(p.X / scale, y, p.Z / scale);
        }

        public static SimVector2 ToSim(Vector3 world)
        {
            int x = Mathf.RoundToInt(world.x * SimConstants.UnitsPerMeter);
            int z = Mathf.RoundToInt(world.z * SimConstants.UnitsPerMeter);
            return new SimVector2(x, z);
        }
    }
}
