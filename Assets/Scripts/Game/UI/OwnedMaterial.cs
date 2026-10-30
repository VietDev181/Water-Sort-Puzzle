using UnityEngine;

/// <summary>Destroys the per-instance material together with the owning object.</summary>
public class OwnedMaterial : MonoBehaviour
{
    public Material Material;

    private void OnDestroy()
    {
        if (Material != null)
            Destroy(Material);
    }
}
