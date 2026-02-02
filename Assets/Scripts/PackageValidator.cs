using UnityEngine;

/// <summary>
/// Validates that total parcel count across all agents equals exactly 10
/// Agent1: 4, Agent2: 3, Agent3: 3
/// </summary>
public class PackageValidator : MonoBehaviour
{
    [Header("Agent References")]
    public ACOTester agent1;
    public ACOTester agent2;
    public ACOTester agent3;

    [Header("Expected Values")]
    public int expectedAgent1Parcels = 4;
    public int expectedAgent2Parcels = 3;
    public int expectedAgent3Parcels = 3;
    public int expectedTotal = 10;

    void Start()
    {
        ValidatePackageCounts();
    }

    public void ValidatePackageCounts()
    {
        int agent1Parcels = agent1 != null ? agent1.TotalParcelCount : 0;
        int agent2Parcels = agent2 != null ? agent2.TotalParcelCount : 0;
        int agent3Parcels = agent3 != null ? agent3.TotalParcelCount : 0;
        int total = agent1Parcels + agent2Parcels + agent3Parcels;

        Debug.Log("=== PACKAGE VALIDATION ===");
        Debug.Log($"Agent1 parcels: {agent1Parcels} (expected: {expectedAgent1Parcels})");
        Debug.Log($"Agent2 parcels: {agent2Parcels} (expected: {expectedAgent2Parcels})");
        Debug.Log($"Agent3 parcels: {agent3Parcels} (expected: {expectedAgent3Parcels})");
        Debug.Log($"Total parcels: {total} (expected: {expectedTotal})");

        bool isValid = true;
        string errors = "";

        if (agent1Parcels != expectedAgent1Parcels)
        {
            isValid = false;
            errors += $"\n  - Agent1 has {agent1Parcels} parcels, should be {expectedAgent1Parcels}";
        }

        if (agent2Parcels != expectedAgent2Parcels)
        {
            isValid = false;
            errors += $"\n  - Agent2 has {agent2Parcels} parcels, should be {expectedAgent2Parcels}";
        }

        if (agent3Parcels != expectedAgent3Parcels)
        {
            isValid = false;
            errors += $"\n  - Agent3 has {agent3Parcels} parcels, should be {expectedAgent3Parcels}";
        }

        if (total != expectedTotal)
        {
            isValid = false;
            errors += $"\n  - Total is {total}, should be {expectedTotal}";
        }

        if (isValid)
        {
            Debug.Log("✓ Package counts are CORRECT!");
        }
        else
        {
            Debug.LogError($"✗ Package counts are INCORRECT!{errors}\n" +
                          $"Fix: Set Parcel Count in ACOTester component Inspector for each agent.");
        }
    }
}

