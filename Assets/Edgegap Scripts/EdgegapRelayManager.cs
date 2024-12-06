using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FishNet.Transporting;
using FishNet.Transporting.KCP.Edgegap;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class EdgegapRelayManager : MonoBehaviour
{
    
    [SerializeField] string relayToken;
    [SerializeField] EdgegapKcpTransport kcpTransport;

    [SerializeField] GameObject partidasUIGameObject;
    [SerializeField] Transform partidaItemContainer;
    [SerializeField] GameObject partidaItemPrefab;

    bool isLocalHost;
    string sessionActualId;
    uint localUserToken;
    
    void Start()
    {
        kcpTransport.OnServerConnectionState += OnServerConnectionStateChange;
        kcpTransport.OnClientConnectionState += OnClientConnectionStateChange;
        RefreshPartidas();
        EdgegapAutoConnect.apiResponse = null;
    }

    public async void CreatePartida()
    {
        //StartCoroutine(GetIP());
        await CrearPartidaAsync();
    }

    void OnServerConnectionStateChange(ServerConnectionStateArgs args)
    {
        switch (args.ConnectionState)
        {
            case LocalConnectionState.Stopped:
                print("Servidor detenido");
                break;
            case LocalConnectionState.Starting:
                break;
            case LocalConnectionState.Started:
                print("Servidor iniciado");
                break;
            case LocalConnectionState.Stopping:
                break;
        }
    }

    void OnClientConnectionStateChange(ClientConnectionStateArgs args)
    {
        switch (args.ConnectionState)
        {
            case LocalConnectionState.Stopped:
                
                // Avisamos que nos desconectamos de la partida
                SalirDePartida();
                
                RefreshPartidas();
                partidasUIGameObject.SetActive(true);
                break;
            case LocalConnectionState.Starting:
                partidasUIGameObject.SetActive(false);
                break;
            case LocalConnectionState.Started:
                break;
            case LocalConnectionState.Stopping:
                break;
        }
    }

    void SalirDePartida()
    {
        if (!string.IsNullOrWhiteSpace(sessionActualId)) // Solo si estoy usando Edgegap
        {
            if (isLocalHost)
            {
                BorrarPartida(sessionActualId);
            }
            else // Solo soy cliente
            {
                AbandonarPartida();
            }
        }
        sessionActualId = null;
    }

    void OnApplicationQuit()
    {
        SalirDePartida();
    }

    const string kEdgegapBaseURL = "https://api.edgegap.com/v1";
    HttpClient httpClient = new HttpClient();

    public async Task CrearPartidaAsync()
    {
        // Preguntamos por nuestra IP publica
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", relayToken);
        HttpResponseMessage responseMessage = await httpClient.GetAsync($"{kEdgegapBaseURL}/ip");
        string response = await responseMessage.Content.ReadAsStringAsync();
        UserIp userIp = JsonUtility.FromJson<UserIp>(response);

        // Creamos lista de usuarios que van jugar en la partida
        Users users = new Users
        {
            users = new List<User>()
        };
        users.users.Add(new User(){ip = userIp.public_ip}); // Nos agregamos nostros mismos
        // Aquí si tenemos un sistema de amigos/party solemos agregar las ips de los demás jugadores

        string usersJson = JsonUtility.ToJson(users);
        HttpContent content = new StringContent(usersJson, Encoding.UTF8, "application/json");
        responseMessage = await httpClient.PostAsync($"{kEdgegapBaseURL}/relays/sessions", content);
        response = await responseMessage.Content.ReadAsStringAsync();
        ApiResponse apiResponse = JsonUtility.FromJson<ApiResponse>(response);
        print("Session: " + apiResponse.session_id);

        while (!apiResponse.ready)
        {
            await Task.Delay(2500); // 2.5 segundos
            responseMessage = await httpClient.GetAsync($"{kEdgegapBaseURL}/relays/sessions/{apiResponse.session_id}");
            response = await responseMessage.Content.ReadAsStringAsync();
            apiResponse = JsonUtility.FromJson<ApiResponse>(response);
        }

        // Este ejemplo esta diseñado, si la escena de multiplayer se encuentra en la misma escena de UI
        //ConectarnosAPartida(apiResponse);

        // Este codigo es para si el NetworkManager se encuentra en diferente escena a este script
        EdgegapAutoConnect.apiResponse = apiResponse;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Odisea"); // La escena que tiene el network manager
    }

    void ConectarnosAPartida(ApiResponse apiResponse)
    {
        // Llegando a este punto, el servidor esta listo para concetarnos

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

    public async void RefreshPartidas()
    {
        await GetTodasLasPartidasAsync();
    }
    
    void ActualizarListaPartidasUI(Sessions sessions)
    {
        // Limpiamos lista actual
        foreach (Transform child in partidaItemContainer) // Recorremos todos los hijos
        {
            Destroy(child.gameObject);
        }

        // Cremaos un item de la lista por cada partida en curso
        foreach (ApiResponse partidaData in sessions.sessions)
        {
            GameObject newItem = Instantiate(partidaItemPrefab, partidaItemContainer);
            PartidaItem partidaItem = newItem.GetComponent<PartidaItem>();
            partidaItem.SetUp(partidaData, this);
        }
    }
    
    async Task GetTodasLasPartidasAsync()
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", relayToken);
        HttpResponseMessage responseMessage = await httpClient.GetAsync($"{kEdgegapBaseURL}/relays/sessions");
        string response = await responseMessage.Content.ReadAsStringAsync();

        Sessions sessions = JsonUtility.FromJson<Sessions>(response);
        ActualizarListaPartidasUI(sessions);
    }

    public async Task UnirPartida(string session_id)
    {
        // Preguntamos por nuestra IP publica
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", relayToken);
        HttpResponseMessage responseMessage = await httpClient.GetAsync($"{kEdgegapBaseURL}/ip");
        string response = await responseMessage.Content.ReadAsStringAsync();
        UserIp userIp = JsonUtility.FromJson<UserIp>(response);
        
        // Aquí si tenemos un sistema de amigos/party solemos agregar las ips de los demás jugadores
        JoinSession joinSessionData = new JoinSession()
        {
            session_id = session_id,
            user_ip = userIp.public_ip
        };
        string usersJson = JsonUtility.ToJson(joinSessionData);
        HttpContent content = new StringContent(usersJson, Encoding.UTF8, "application/json");
        responseMessage = await httpClient.PostAsync($"{kEdgegapBaseURL}/relays/sessions:authorize-user", content);
        response = await responseMessage.Content.ReadAsStringAsync();
        ApiResponse apiResponse = JsonUtility.FromJson<ApiResponse>(response);
        
        // Esta funcion esta diseñada para si el NetworkManager esta en la misma escena que este script
        //ConectarnosAPartida(apiResponse);
        
        // Codigo si el NetworkManager esta en OTRA escena
        EdgegapAutoConnect.apiResponse = apiResponse;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Odisea"); // La escena que tiene el network manager
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

    async Task BorrarPartida(string session_id)
    {
        HttpResponseMessage responseMessage = await httpClient.DeleteAsync($"{kEdgegapBaseURL}/relays/sessions/{session_id}");
        string response = await responseMessage.Content.ReadAsStringAsync();
    }

    [ContextMenu("Borrar todas las partidas")]
    async void DevBorrarTodasPartidas()
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", relayToken);
        HttpResponseMessage responseMessage = await httpClient.GetAsync($"{kEdgegapBaseURL}/relays/sessions");
        string response = await responseMessage.Content.ReadAsStringAsync();

        Sessions sessions = JsonUtility.FromJson<Sessions>(response);
        foreach (ApiResponse partida in sessions.sessions)
        {
            await BorrarPartida(partida.session_id);
        }
        print("Todas las partidas fueron borradas");
    }
    
}
