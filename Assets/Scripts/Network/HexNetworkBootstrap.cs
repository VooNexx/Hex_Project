using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Photon Fusion 2 bağlantı yöneticisi.
/// Sahnede bir NetworkRunner başlatır ve Host veya Client olarak bağlanmayı sağlar.
/// </summary>
public class HexNetworkBootstrap : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private string _sessionName = "HexRoom";

    private void OnGUI()
    {
        if (_runner == null)
        {
            // Basit ve şık bir GUI tasarımı
            GUI.Box(new Rect(10, 10, 220, 180), "Photon Fusion 2");
            GUILayout.BeginArea(new Rect(20, 35, 200, 150));
            
            GUILayout.Label("Oda Adı:");
            _sessionName = GUILayout.TextField(_sessionName);

            GUILayout.Space(10);

            if (GUILayout.Button("Host Olarak Başlat (Server)", GUILayout.Height(30)))
            {
                StartGame(GameMode.Host);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Client Olarak Katıl", GUILayout.Height(30)))
            {
                StartGame(GameMode.Client);
            }
            GUILayout.EndArea();
        }
        else
        {
            GUI.Box(new Rect(10, 10, 220, 100), "Bağlantı Durumu");
            GUILayout.BeginArea(new Rect(20, 35, 200, 70));
            GUILayout.Label($"Mod: {_runner.GameMode}");
            GUILayout.Label($"Oda: {_runner.SessionInfo.Name}");
            if (GUILayout.Button("Bağlantıyı Kes"))
            {
                _runner.Shutdown();
            }
            GUILayout.EndArea();
        }
    }

    private async void StartGame(GameMode mode)
    {
        // Eski runner varsa temizle
        if (_runner != null)
        {
            Destroy(_runner.gameObject);
        }

        // Yeni runner objesi oluştur
        GameObject runnerObject = new GameObject("FusionRunner");
        _runner = runnerObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Callback'leri kaydet
        _runner.AddCallbacks(this);

        // Mevcut sahneyi yükle
        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = _sessionName,
            Scene = sceneRef,
            SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log($"[FusionBootstrap] Oyun başarıyla başlatıldı: {mode}");
        }
        else
        {
            Debug.LogError($"[FusionBootstrap] Oyun başlatılamadı: {result.ShutdownReason}");
            Destroy(runnerObject);
            _runner = null;
        }
    }

    // --- INetworkRunnerCallbacks Arayüzü ---

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[HexNetworkBootstrap] Oyuncu katıldı: {player}");

        // NetworkGameManager'a oyuncu katılımını bildir
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.ServerPlayerJoined(player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[HexNetworkBootstrap] Oyuncu ayrıldı: {player}");

        // NetworkGameManager'a oyuncu ayrılışını bildir
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.ServerPlayerLeft(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[FusionBootstrap] Kapatıldı: {shutdownReason}");
        if (_runner != null)
        {
            Destroy(_runner.gameObject);
            _runner = null;
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[FusionBootstrap] Sunucuya bağlanıldı.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[FusionBootstrap] Sunucu bağlantısı kesildi: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[FusionBootstrap] Bağlantı hatası: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[FusionBootstrap] Sahne yüklemesi tamamlandı.");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[FusionBootstrap] Sahne yüklemesi başladı.");
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
