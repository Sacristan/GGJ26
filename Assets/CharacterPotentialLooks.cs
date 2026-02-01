using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
[CreateAssetMenu(fileName = "Look", menuName = "ScriptableObjects/Look", order = 1)]
public class CharacterPotentialLooks : ScriptableObject
{
    [Serializable]
    public class Look
    {
        [SerializeField] private Material[] materials;
        public Material GetMaterial() => materials[UnityEngine.Random.Range(0, materials.Length)];
    }

    [SerializeField] private Look bodyFine;
    [SerializeField] private Look bodyInfected;

    [SerializeField] private Look trousers;

    [SerializeField] public Look hairLook;

    [SerializeField] public Look eyesFine;
    [SerializeField] public Look eyesInfected;

    [SerializeField] public Look shirtLook;

    [Range(0f, 1f)] [SerializeField] private float chanceToBeInfected = 0.35f;

    Look GetBody(bool isInfected) => isInfected ? bodyInfected : bodyFine;
    Look GetTrousers(bool isInfected) => trousers;
    Look GetHair(bool isInfected) => hairLook;
    Look GetEyes(bool isInfected) => isInfected ? eyesInfected : eyesFine;
    Look GetShirt(bool isInfected) => shirtLook;

    public (bool, Material[]) Randomise()
    {
        bool isInfected = Random.value < chanceToBeInfected;

        Material[] materials = new[]
        {
            GetBody(isInfected).GetMaterial(),
            GetTrousers(isInfected).GetMaterial(),
            GetHair(isInfected).GetMaterial(),
            GetEyes(isInfected).GetMaterial(),
            GetShirt(isInfected).GetMaterial()
        };

        return (isInfected, materials);
    }
}