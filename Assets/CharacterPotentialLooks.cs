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

    private bool isInfected = false;

    Look Body => isInfected ? bodyInfected : bodyFine;
    Look Trousers => trousers;
    Look Hair => hairLook;
    Look Eyes => isInfected ? eyesInfected : eyesFine;
    Look Shirt => shirtLook;

    public Material[] Randomise()
    {
        isInfected = Random.value < 0.5f;

        Material[] materials = new[]
        {
            Body.GetMaterial(),
            Trousers.GetMaterial(),
            Hair.GetMaterial(),
            Eyes.GetMaterial(),
            Shirt.GetMaterial()
        };

        return materials;
    }
}