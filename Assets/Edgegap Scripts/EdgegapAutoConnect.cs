using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FishNet.Transporting;
using FishNet.Transporting.KCP.Edgegap;
using UnityEngine;


// debe estar en el network manager
public class EdgegapAutoConnect : MonoBehaviour
{
    public static ApiResponse apiResponse;
    [SerializeField] string relayToken;
    [SerializeField] EdgegapKcpTransport kcpTransport;
    bool isLocalHost;
    uint localUserToken;
    string sessionActualId;
    
    const string kEdgegapBaseURL = "https://api.edgegap.com/v1";
    HttpClient httpClient = new HttpClient();
    
    void Start()
    {
        if (apiResponse == null)
            return;
        
        kcpTransport.OnClientConnectionState += OnClientConnectionStateChange;
        
        // Codigo de ConectarnosAPartida de EdgegapRelayManager.cs
        uint userToken = 0;
        if (apiResponse.session_users != null)
        {
            userToken = apiResponse.session_users[0].authorization_token;
            isLocalHost = true;
        }
        else
        {
            userToken = apiResponse.session_user.authorization_token;
            isLocalHost = false;
        }
        localUserToken = userToken;
        
        EdgegapRelayData relayData = new EdgegapRelayData(
            apiResponse.relay.ip,
            apiResponse.relay.ports.server.port,
            apiResponse.relay.ports.client.port,
            userToken, // Token de autorización del usuario/jugador
            apiResponse.authorization_token // Token para conectarnos al server
        );
        sessionActualId = apiResponse.session_id;
        
        kcpTransport.SetEdgegapRelayData(relayData);
        if(isLocalHost) // Si soy el primer jugador, significa que tambien soy el server
            kcpTransport.StartConnection(true); // Nos conectamos como servidor
        kcpTransport.StartConnection(false); // Nos conectamos como cliente (Nos convertimos en host)
    }

    void OnDestroy()
    {
        if(kcpTransport)
            kcpTransport.OnClientConnectionState -= OnClientConnectionStateChange;
    }

    void OnClientConnectionStateChange(ClientConnectionStateArgs args)
    {
        switch (args.ConnectionState)
        {
            case LocalConnectionState.Stopped:
                // Avisamos que nos desconectamos de la partida
                SalirDePartida();
                break;
            case LocalConnectionState.Starting:
                break;
            case LocalConnectionState.Started:
                break;
            case LocalConnectionState.Stopping:
                break;
        }
    }
    
    async Task SalirDePartida()
    {
        if (!string.IsNullOrWhiteSpace(sessionActualId)) // Solo si estoy usando Edgegap
        {
            if (isLocalHost)
            {
                await BorrarPartida(sessionActualId);
            }
            else // Solo soy cliente
            {
                await AbandonarPartida();
            }
        }
        sessionActualId = null;
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby"); // Cargar la escena que tiene el UI
    }
    
    async Task BorrarPartida(string session_id)
    {
        HttpResponseMessage responseMessage = await httpClient.DeleteAsync($"{kEdgegapBaseURL}/relays/sessions/{session_id}");
        string response = await responseMessage.Content.ReadAsStringAsync();
    }
    
    async Task AbandonarPartida()
    {
        LeaveSession leaveSessionData = new LeaveSession()
        {
            session_id = sessionActualId,
            authorization_token = localUserToken
        };
        
        string leaveSessionJson = JsonUtility.ToJson(leaveSessionData);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", relayToken);
        HttpContent content = new StringContent(leaveSessionJson, Encoding.UTF8, "application/json");
        await httpClient.PostAsync($"{kEdgegapBaseURL}/relays/sessions:revoke-user", content);
    }
}
