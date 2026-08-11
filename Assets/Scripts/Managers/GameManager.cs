using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public enum PlayerRole { Scout, Rescuer, Medic }

    // Reference to the three player GameObjects (assign in Inspector)
    [Header("Players")]
    public GameObject scoutPlayer;
    public GameObject rescuerPlayer;
    public GameObject medicPlayer;

    // Internal array to hold the player GameObjects for easy access
    private PlayerMovement[] playerMovements;
    [HideInInspector] public InteractionHandler[] interactionHandlers;

    [field: Header("Mission Settings")]
    [field: SerializeField] public int totalSurvivors { get; private set; } // set in Inspector
    [field: SerializeField] public float levelTime = 300f;           // 5 minutes
    [field: SerializeField] public int rescuedCount { get; private set; }
    [field: SerializeField] public int stabilisedCount { get; private set; }
    [field: SerializeField] public int correctTagCount = 0; // Count of survivors tagged correctly by Scout
    [field: SerializeField] public int incorrectTagCount = 0; // Count of survivors tagged incorrectly by Scout
    [field: SerializeField] public float timer { get; private set; }
    [field: SerializeField] public bool gameEnded { get; private set; } = false;

    public PlayerRole currentRole { get; private set; }

    void Awake()
    {
        Instance = this;

        // Choose role from session data, fallback to Scout
        currentRole = SessionData.SelectedRole ?? PlayerRole.Scout;
        SessionData.SelectedRole = null; // clear for next game


        // Cache movement and interaction components for each player
        playerMovements = new PlayerMovement[3];
        interactionHandlers = new InteractionHandler[3];

        playerMovements[0] = scoutPlayer.GetComponent<PlayerMovement>();
        playerMovements[1] = rescuerPlayer.GetComponent<PlayerMovement>();
        playerMovements[2] = medicPlayer.GetComponent<PlayerMovement>();

        interactionHandlers[0] = scoutPlayer.GetComponent<InteractionHandler>();
        interactionHandlers[1] = rescuerPlayer.GetComponent<InteractionHandler>();
        interactionHandlers[2] = medicPlayer.GetComponent<InteractionHandler>();
    }
    void Start()
    {
        timer = levelTime;

        // Only activate the chosen role’s player
        for (int i = 0; i < 3; i++)
        {
            bool active = (i == (int)currentRole);
            playerMovements[i].SetActive(active);
            interactionHandlers[i].SetActive(active);
        }

        // Camera follow
        CameraController.Instance?.SetTarget(GetActivePlayerTransform());

    }

    private void Update()
    {
        if (gameEnded) return;

        timer -= Time.deltaTime;

        // Lose condition
        if (timer <= 0)
        {
            EndGame(false);
        }
        // Win condition
        else if (AllSurvivorsHandled())
        {
            EndGame(true);
        }
    }

    bool AllSurvivorsHandled()
    {
        // Check all survivors that are NOT Red have been rescued AND stabilised.
        Survivor[] all = FindObjectsOfType<Survivor>();
        foreach (Survivor s in all)
        {
            if (s.tagColor == TagColor.Red) continue;   // Red are lost, ignore
            if (!s.isRescued || !s.isStable)
                return false;
        }
        return true;
    }

    // Called by Survivor when dropped in Safe Zone
    public void OnSurvivorRescued(Survivor survivor)
    {
        rescuedCount++;

        // Tag correctness check
        if (survivor.GetCorrectTag() == survivor.tagColor)
            correctTagCount++;
        else
            incorrectTagCount++;
    }

    // Called by Survivor when fully healed
    public void OnSurvivorStabilised(Survivor s)
    {
        stabilisedCount++;
    }

    void EndGame(bool won)
    {
        gameEnded = true;
        // Calculate score
        int score = (rescuedCount * 100) + (stabilisedCount * 50)
                    + (int)(timer * 10) + (correctTagCount * 30);

        // Notify UI
        UIManager.Instance.ShowEndScreen(won, score);
    }

    public Transform GetActivePlayerTransform()
    {
        return playerMovements[(int)currentRole].transform;
    }
}
