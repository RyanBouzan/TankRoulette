using UnityEngine;
using TMPro; // Import TextMeshPro
using FishNet.Managing;
using FishNet.Managing.Statistic;
using FishNet.Managing.Timing;
using FishNet;

public class NetworkStatsDisplay : MonoBehaviour
{
    public TextMeshProUGUI statsText; // Reference to the TextMeshProUGUI component
    [SerializeField]
    private NetworkManager networkManager;

    [SerializeField]
    private TimeManager timeManager;

    [SerializeField]
    private int tickCount;

    [SerializeField]
    private NetworkTraficStatistics networkTrafficStatistics;
    [SerializeField]
    private bool _showIncoming;
    [SerializeField]
    private bool _showOutgoing;
    private string _clientText;
    private string _serverText;

    void Start()
    {
        // Get the NetworkManager instance
        networkManager = InstanceFinder.NetworkManager;
        
        // Start updating the stats UI every second
        
        networkTrafficStatistics = InstanceFinder.NetworkManager.StatisticsManager.NetworkTraffic;
            //Subscribe to both traffic updates.
            networkTrafficStatistics.OnClientNetworkTraffic += NetworkTraffic_OnClientNetworkTraffic;
            networkTrafficStatistics.OnServerNetworkTraffic += NetworkTraffic_OnServerNetworkTraffic;

            if (!networkTrafficStatistics.UpdateClient && !networkTrafficStatistics.UpdateServer)
                Debug.LogWarning($"StatisticsManager.NetworkTraffic is not updating for client nor server. To see results ensure your NetworkManager has a StatisticsManager component added with the NetworkTraffic values configured.");

        timeManager = networkManager.GetComponent<TimeManager>();
        if(InstanceFinder.IsClientOnly)
        timeManager.SetTickRate(90);
        timeManager.OnTick += onTick;

        InvokeRepeating("UpdateStatsUI", 1f, 1f);

    }

    private void onTick()
    {
        tickCount++;
    }

     private void OnDestroy()
        {
            if (networkTrafficStatistics != null)
            {
                networkTrafficStatistics.OnClientNetworkTraffic -= NetworkTraffic_OnClientNetworkTraffic;
                networkTrafficStatistics.OnServerNetworkTraffic -= NetworkTraffic_OnServerNetworkTraffic;
            }
            timeManager.OnTick -= onTick;

        }


    private void NetworkTraffic_OnClientNetworkTraffic(NetworkTrafficArgs obj)
        {
            string nl = System.Environment.NewLine;
            string result = string.Empty;
            if (_showIncoming)
                result += $"Client In: {NetworkTraficStatistics.FormatBytesToLargest(obj.FromServerBytes)}/s{nl}";
            if (_showOutgoing)
                result += $"Client Out: {NetworkTraficStatistics.FormatBytesToLargest(obj.ToServerBytes)}/s{nl}";

            _clientText = result;
        }

        /// <summary>
        /// Called when client network traffic is updated.
        /// </summary>
        private void NetworkTraffic_OnServerNetworkTraffic(NetworkTrafficArgs obj)
        {
            string nl = System.Environment.NewLine;
            string result = string.Empty;
            if (_showIncoming)
                result += $"Server In: {NetworkTraficStatistics.FormatBytesToLargest(obj.ToServerBytes)}/s{nl}";
            if (_showOutgoing)
                result += $"Server Out: {NetworkTraficStatistics.FormatBytesToLargest(obj.FromServerBytes)}/s{nl}";

            _serverText = result;
        }

    void UpdateStatsUI()
    {
        // Check if network manager and statistics are available
        if (networkManager == null ||  networkTrafficStatistics == null) return;

        TimeManager tm = networkManager.GetComponent<TimeManager>();

        statsText.text = "Network Traffic Statistics\n" +
            "Ping: " + tm.RoundTripTime + " ms\n" + 
            "Tick rate: " + tickCount + " \n" +
            _clientText +
            _serverText;
        tickCount = 0;
    }
}
