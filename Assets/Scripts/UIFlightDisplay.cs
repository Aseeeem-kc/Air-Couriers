using UnityEngine;
using TMPro;

public class UIFlightDisplay : MonoBehaviour
{
    [Header("Airplane Reference")]
    public PathfindingTester plane;  // Drag your airplane here

    [Header("UI Text Elements")]
    public TMP_Text SpeedText;
    public TMP_Text FuelText;
    public TMP_Text CargoText;
    public TMP_Text FuelRateText;    // NEW: Text for fuel consumption rate

    [Header("Flight Settings")]
    public float maxFuel = 100f;
    public float fuelConsumptionRate = 2f;      // base fuel rate
    public float reducedFuelRate = 1f;          // rate when at goal
    public float increasedFuelRate = 2f;        // rate when at start

    private float currentFuel;
    private bool cargoLoaded = true;

    void Start()
    {
        currentFuel = maxFuel;

        if (plane == null)
            Debug.LogWarning("Plane reference is missing in UIFlightDisplay.");

        if (SpeedText == null || FuelText == null || CargoText == null || FuelRateText == null)
            Debug.LogWarning("One or more UI Text fields are not assigned.");
    }

    void Update()
    {
        if (plane == null) return;

        // Update speed
        if (SpeedText != null)
            SpeedText.text = $"Speed: {plane.CurrentSpeed:0}";

        // Update fuel consumption rate dynamically
        if (plane.HasPausedAtGoal)
            fuelConsumptionRate = reducedFuelRate;    // decrease rate at end
        else if (plane.CurrentTargetIsStart())      // custom method to check if at start
            fuelConsumptionRate = increasedFuelRate;

        // Update fuel
        if (FuelText != null)
        {
            currentFuel -= Time.deltaTime * fuelConsumptionRate;
            if (currentFuel < 0) currentFuel = 0;
            FuelText.text = $"Fuel: {currentFuel:0}";
        }

        // Update cargo
        if (CargoText != null)
        {
            if (plane.HasPausedAtGoal && cargoLoaded)
                cargoLoaded = false;

            CargoText.text = $"Cargo: {(cargoLoaded ? "Loaded" : "Off")}";
        }

        // Update fuel rate text
        if (FuelRateText != null)
            FuelRateText.text = $"Fuel Rate: {fuelConsumptionRate:0.0}/s";
    }

    // Optional: Reset Flight
    public void ResetFlight()
    {
        currentFuel = maxFuel;
        cargoLoaded = true;
        fuelConsumptionRate = increasedFuelRate;
    }
}
