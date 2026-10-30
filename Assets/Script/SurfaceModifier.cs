using UnityEngine;

public enum SurfaceType
{
    NormalDrySoil,
    MudPeatBog
}

public class SurfaceModifier : MonoBehaviour
{
    [Header("Surface Properties")]
    public SurfaceType surfaceType = SurfaceType.MudPeatBog;

    [Range(0f, 1f)]
    [Tooltip("Persentase pengurangan kecepatan (misal: 0.35 = melambat 35%)")]
    public float speedDragPenalty = 0.35f;

    public float GetSpeedMultiplier()
    {
        return 1f - speedDragPenalty;
    }
}
