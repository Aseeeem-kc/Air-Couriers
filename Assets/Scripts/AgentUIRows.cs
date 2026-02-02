using TMPro;
using UnityEngine;

public class AgentUIRow : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI agentNameText;
    public TextMeshProUGUI alphaText;
    public TextMeshProUGUI betaText;
    public TextMeshProUGUI qText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI packageText;
    public TextMeshProUGUI statusText;

    // Update row for ACOTester agent
    public void UpdateRow(ACOTester agent)
    {
        if (agent == null) return;

        if (agentNameText != null)
            agentNameText.text = agent.gameObject.name;

        if (alphaText != null)
            alphaText.text = agent.Alpha.ToString("F1");

        if (betaText != null)
            betaText.text = agent.Beta.ToString("F4");

        if (qText != null)
            qText.text = agent.QValue.ToString("F4");

        if (speedText != null)
            speedText.text = $"{agent.CurrentSpeed:F2} m/s";

        if (distanceText != null)
            distanceText.text = $"{agent.TotalDistance:F1} m";

        if (packageText != null)
        {
            // Show remaining/total for clearer \"package status\"
            packageText.text = $"{agent.ParcelCount}/{agent.TotalParcelCount}";
        }

        if (statusText != null)
            statusText.text = agent.DeliveryStatus;
    }

    // Update row for PathfindingTester agent (A* phase)
    public void UpdateRow(PathfindingTester agent)
    {
        if (agent == null) return;

        if (agentNameText != null)
            agentNameText.text = agent.gameObject.name;

        // A* agents don't have ACO parameters, show default/zero values
        if (alphaText != null)
            alphaText.text = "-";

        if (betaText != null)
            betaText.text = "-";

        if (qText != null)
            qText.text = "-";

        if (speedText != null)
            speedText.text = $"{agent.CurrentSpeed:F2} m/s";

        if (distanceText != null)
            distanceText.text = $"{agent.TotalDistance:F1} m";

        if (packageText != null)
            packageText.text = agent.PackageCount.ToString();

        if (statusText != null)
            statusText.text = agent.GetDeliveryStatus();
    }
}
