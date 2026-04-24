using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Test script for Phase 8 screens
/// Attach to a GameObject in test scene
/// Creates simple buttons to test each screen
/// </summary>
public class TestPhase8 : MonoBehaviour
{
    [Header("Screen References")]
    public SoldierInventoryScreen soldierScreen;
    public DivisionsListScreen divisionsScreen;
    public DivisionDetailScreen divisionDetailScreen;
    public ProfileScreen profileScreen;

    [Header("Test Buttons")]
    public Button testSoldierButton;
    public Button testDivisionsListButton;
    public Button testDivisionDetailButton;
    public Button testProfileButton;

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (testSoldierButton != null)
        {
            testSoldierButton.onClick.AddListener(TestSoldierScreen);
        }

        if (testDivisionsListButton != null)
        {
            testDivisionsListButton.onClick.AddListener(TestDivisionsListScreen);
        }

        if (testDivisionDetailButton != null)
        {
            testDivisionDetailButton.onClick.AddListener(TestDivisionDetailScreen);
        }

        if (testProfileButton != null)
        {
            testProfileButton.onClick.AddListener(TestProfileScreen);
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // Test Methods
    // ───────────────────────────────────────────────────────────────────

    public void TestSoldierScreen()
    {
        Debug.Log("[TestPhase8] Testing Soldier Screen...");

        if (soldierScreen == null)
        {
            Debug.LogError("[TestPhase8] SoldierInventoryScreen reference is null!");
            return;
        }

        if (ManifestLoader.Instance == null || !ManifestLoader.Instance.IsLoaded)
        {
            Debug.LogError("[TestPhase8] ManifestLoader not ready!");
            return;
        }

        // Get first soldier from manifest
        var soldiers = ManifestLoader.Instance.GetAllSoldiers();
        if (soldiers.Count == 0)
        {
            Debug.LogError("[TestPhase8] No soldiers found in manifest!");
            return;
        }

        string testSoldierId = soldiers[0].id;
        Debug.Log($"[TestPhase8] Showing soldier: {testSoldierId} - {soldiers[0].name}");

        soldierScreen.ShowSoldier(testSoldierId);
    }

    public void TestDivisionsListScreen()
    {
        Debug.Log("[TestPhase8] Testing Divisions List Screen...");

        if (divisionsScreen == null)
        {
            Debug.LogError("[TestPhase8] DivisionsListScreen reference is null!");
            return;
        }

        divisionsScreen.PopulateScreen();
    }

    public void TestDivisionDetailScreen()
    {
        Debug.Log("[TestPhase8] Testing Division Detail Screen...");

        if (divisionDetailScreen == null)
        {
            Debug.LogError("[TestPhase8] DivisionDetailScreen reference is null!");
            return;
        }

        if (ManifestLoader.Instance == null || !ManifestLoader.Instance.IsLoaded)
        {
            Debug.LogError("[TestPhase8] ManifestLoader not ready!");
            return;
        }

        // Get first division from manifest
        var divisions = ManifestLoader.Instance.GetAllDivisions();
        if (divisions.Count == 0)
        {
            Debug.LogError("[TestPhase8] No divisions found in manifest!");
            return;
        }

        string testDivisionId = divisions[0].id;
        Debug.Log($"[TestPhase8] Showing division: {testDivisionId} - {divisions[0].name}");

        divisionDetailScreen.ShowDivision(testDivisionId);
    }

    public void TestProfileScreen()
    {
        Debug.Log("[TestPhase8] Testing Profile Screen...");

        if (profileScreen == null)
        {
            Debug.LogError("[TestPhase8] ProfileScreen reference is null!");
            return;
        }

        profileScreen.PopulateScreen();
    }
}