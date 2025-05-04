using System.Collections;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using FishNet;
using TMPro;
using UnityEngine.UI;
using FishNet.Managing.Scened;
public class ServerController : MonoBehaviour
{
    public TextMeshProUGUI inputFieldText;
    public string username;
    public string joinCode;
    public UnityTransport transport;
    public NetworkManager networkManager;
    int errorNums = 0;
    public void setUser(string userN)
    {
        username = userN;
    }
    public async void startHost()
    {
        await UnityServices.InitializeAsync();
        if(!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(3);
        string joinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        GUIUtility.systemCopyBuffer = joinCode;
        transport.SetRelayServerData(new RelayServerData(allocation, connectionType:"dtls"));

        if(networkManager.ServerManager.StartConnection())
        {
            networkManager.ClientManager.StartConnection();
            Debug.Log("hosted");
            switchScene();              
        }
    
    }

    public async void joinServer()
    {
        joinCode = inputFieldText.text;

        await UnityServices.InitializeAsync();
        if(!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        JoinAllocation allocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(inputFieldText.text.Substring(0,6));
        transport.SetRelayServerData(new RelayServerData(allocation, connectionType:"dtls"));

        if(networkManager.ClientManager.StartConnection())
        {
            Debug.Log($"joined {joinCode}");
            switchScene();        
        }
    }

    public void switchScene()
    {
        SceneLoadData sld = new SceneLoadData("Game");    
        sld.ReplaceScenes = ReplaceOption.All;
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);    
    }
}
