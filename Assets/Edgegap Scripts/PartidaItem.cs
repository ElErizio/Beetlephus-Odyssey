using TMPro;
using UnityEngine;

public class PartidaItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nombrePartida;
    [SerializeField] TextMeshProUGUI numeroJugadores;
    EdgegapRelayManager _edgegapRelayManager;

    public void SetUp(ApiResponse apiResponse, EdgegapRelayManager relayManager)
    {
        nombrePartida.text = apiResponse.session_id;
        numeroJugadores.text = apiResponse.session_users.Length.ToString();
        _edgegapRelayManager = relayManager;
    }

    public async void UnirPartida()
    {
       await _edgegapRelayManager.UnirPartida(nombrePartida.text);
    }

    
}
