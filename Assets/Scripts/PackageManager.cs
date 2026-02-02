using UnityEngine;

public class PackageManager : MonoBehaviour
{
    public static PackageManager Instance;

    public int totalPackages = 10;
    private int remainingPackages;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            remainingPackages = totalPackages;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Reset on scene start to ensure consistency
        remainingPackages = totalPackages;
        Debug.Log($"[PackageManager] Reset: Total packages = {totalPackages}, Remaining = {remainingPackages}");
    }

    // Assign packages to an agent (for backward compatibility)
    public int AssignPackages(int amount)
    {
        if (remainingPackages <= 0)
            return 0;

        int assigned = Mathf.Min(amount, remainingPackages);
        remainingPackages -= assigned;
        return assigned;
    }

    // Assign fixed number of packages (for Part 3 requirements)
    public int AssignFixedPackages(int fixedAmount)
    {
        if (remainingPackages <= 0)
            return 0;

        int assigned = Mathf.Min(fixedAmount, remainingPackages);
        remainingPackages -= assigned;
        return assigned;
    }

    public int RemainingPackages => remainingPackages;
}
