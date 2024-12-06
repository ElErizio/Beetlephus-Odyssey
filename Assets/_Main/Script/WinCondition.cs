using FishNet;
using FishNet.Managing.Logging;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinCondition : NetworkBehaviour
{
    public GameObject ImagenVictoria;

    [Server(Logging = LoggingType.Off)]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BolaCaca"))
        {
            ImagenVictoria.SetActive(true);
            RpcShowVictoryImage();
            /* SceneLoadData sceneLoadData = new SceneLoadData("Victoria");
            sceneLoadData.ReplaceScenes = ReplaceOption.All;
            sceneLoadData.MovedNetworkObjects = PersonajesManager.Instance.personajeObjects.ToArray();

            InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);*/
        }
    }

    [ObserversRpc]
    private void RpcShowVictoryImage()
    {
        if (ImagenVictoria != null)
        {
            ImagenVictoria.SetActive(true);
        }
    }
}
