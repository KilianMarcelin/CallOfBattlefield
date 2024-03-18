using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{

    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private TMP_InputField ipField;

    // Joining a hosted game
    public void join() 
    {
        _networkManager.networkAddress = ipField.text; // Get ip entered by user
        _networkManager.StartClient(); // Starting client
    }
    
    // Hosting a game
    public void host()
    {
        _networkManager.StartHost();
        Debug.Log(_networkManager.networkAddress);
    }
}
