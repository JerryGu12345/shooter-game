using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetcodeForEntitiesUI : MonoBehaviour
{
    [SerializeField] private Button startServerButton;
    [SerializeField] private Button joinGameButton;

    private void Awake() {
        startServerButton.onClick.AddListener(StartServer);
        joinGameButton.onClick.AddListener(JoinGame);
    }

    private void StartServer() {
        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        //World clientWorld = ClientServerBootstrap.CreateClientWorld("clientWorld");

        foreach (World world in World.All) {
            if (world.Flags == WorldFlags.Game) {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null) {
            World.DefaultGameObjectInjectionWorld=serverWorld;
        }

        //SceneManager.LoadSceneAsync("SampleScene",LoadSceneMode.Single);
        // //SceneManager.LoadSceneAsync("EntitiesSubscene",LoadSceneMode.Additive);
        ushort port = 7979;
        
        serverWorld.EntityManager
            .CreateEntityQuery(typeof(NetworkStreamDriver))
            .GetSingleton<NetworkStreamDriver>()
            .Listen(NetworkEndpoint.AnyIpv4.WithPort(port));
/*
        NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.LoopbackIpv4.WithPort(port);
        networkStreamDriver = 
            clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver))
            .GetSingleton<NetworkStreamDriver>();
        networkStreamDriver.Connect(clientWorld.EntityManager, connectNetworkEndpoint);*/
    }

    private void JoinGame () {
        World clientWorld = ClientServerBootstrap.CreateClientWorld("clientWorld");

        foreach (World world in World.All) {
            if (world.Flags == WorldFlags.Game) {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null) {
            World.DefaultGameObjectInjectionWorld=clientWorld;
        }

        SceneManager.LoadSceneAsync("SampleScene",LoadSceneMode.Single);
        //SceneManager.LoadSceneAsync("EntitiesSubscene",LoadSceneMode.Additive);
        ushort port = 7979;
        string ip = "127.0.0.1";

        NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.Parse(ip,port);
        clientWorld.EntityManager
            .CreateEntityQuery(typeof(NetworkStreamDriver))
            .GetSingleton<NetworkStreamDriver>()
            .Connect(clientWorld.EntityManager, connectNetworkEndpoint);
    }
}
