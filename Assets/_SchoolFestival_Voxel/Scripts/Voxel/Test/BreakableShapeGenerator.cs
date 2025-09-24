using UnityEngine;

public static class BreakableShapeGenerator
{
    public static float[,,] GenerateCubeDensity(int sizeX, int sizeY, int sizeZ, int margin)
    {
        float[,,] d = new float[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    bool inside = x >= margin && x < sizeX - margin && y >= margin && y < sizeY - margin && z >= margin && z < sizeZ - margin;
                    d[x, y, z] = inside ? 1f : -1f;
                }
        return d;
    }

    public static float[,,] GenerateTorusDensity(int size, float R, float r, float voxelSize = 1f)
    {
        float[,,] d = new float[size, size, size];
        float center = (size - 1) / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    float fx = (x - center) * voxelSize;
                    float fy = (y - center) * voxelSize;
                    float fz = (z - center) * voxelSize;

                    float qx = Mathf.Sqrt(fx * fx + fz * fz) - R;
                    float qy = fy;
                    float dist2 = qx * qx + qy * qy;

                    d[x, y, z] = (r * r) - dist2; // positive inside the torus tube
                }
            }
        }
        return d;
    }
}
