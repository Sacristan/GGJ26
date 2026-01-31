using UnityEngine;

public class CharacterLook : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer skinnedMeshRenderer;

    public void SetMaterials(Material[] materials)
    {
        skinnedMeshRenderer.materials = materials;
    }
}
