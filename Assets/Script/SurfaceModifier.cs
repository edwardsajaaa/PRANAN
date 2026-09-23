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
    [Tooltip("Persentase pengurangan kecepatan dan akselerasi (35% default sesuai W1-04)")]
    public float speedDragPenalty = 0.35f;

    public float GetSpeedMultiplier()
    {
        return 1f - speedDragPenalty;
    }
}
