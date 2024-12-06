using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class PersonajesManager : MonoBehaviour
{
    public static PersonajesManager Instance {get; private set;}

    public List<NetworkObject> personajeObjects = new List<NetworkObject>();
    
    private void Awake()
    {
        Instance = this;
    }   
}
