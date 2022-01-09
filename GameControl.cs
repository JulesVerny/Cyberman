using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class GameControl : MonoBehaviour
{
    //==========================================================================
    public Text ScoreTextDisplay;
    public Text LevelTextDisplay;
    public Text NarrativeTextDisplay;

    //public GameObject TheCyberman;
    public GameObject TheDalek;
    public GameObject YellowPlatform;
    public GameObject GreenPlatform;
    public GameObject RudiumReactor;

    public GameObject[] PlayerStartPositions;
    public GameObject[] DalekStartPositions;
    public GameObject[] RudiumStartPositions;
    public GameObject[] RudiumGoalPositions;

    public int TestGameLevel;

    //private int ScenarioLevel;
    private int DalekScore;
    private int CyberManScore;
    private Rigidbody RudiumRB; 
    private int PreviousGameLevel;

    private string NarrativeString;

    //==========================================================================
    void Start()
    {
        DalekScore = 0;
        CyberManScore = 0;
        PreviousGameLevel = 0; 

        RudiumRB = RudiumReactor.GetComponent<Rigidbody>(); 

        UpdateScoreDisplay();

        HideAllSpawnObjects(); 

    } // Start
    //==========================================================================
    public Vector3 ResetTheEpisode(int CurrentGameLevel)
    {
        // Default Positions
        Vector3 CybermanStartPosition = this.transform.position;
        Vector3 DalekStartPosition = this.transform.position;
        Vector3 YellowPlatformPosition = this.transform.position;
        Vector3 GreenPlatformPosition = this.transform.position;
        Vector3 RudiumPosition = this.transform.position;

        // For debug the spawn Postions 
       // CurrentGameLevel = TestGameLevel; 

        if(CurrentGameLevel> PreviousGameLevel)
        {
            // A Change in Game Level  - Reset the Scores
            DalekScore = 0;
            CyberManScore = 0;
            UpdateScoreDisplay();
            PreviousGameLevel = CurrentGameLevel; 
        }

        //  LerpRandomSpawnPosition(int GameLevel, float SpawnHeight, Vector3 V1, Vector3 V2, Vector3 V3, Vector3 V4)
        CybermanStartPosition = LerpRandomSpawnPosition(CurrentGameLevel, 0.65f, new Vector3(-22.5f, 0.0f, 5.0f), new Vector3(-12.5f, 0.0f, 5.0f), new Vector3(-10.0f, 0.0f, -15.0f), new Vector3(-22.5f, 0.0f, -20.0f));

        DalekStartPosition = RandomDalekSpawnNorth(new Vector3(15.0f, 0.0f, 20.0f), new Vector3(15.0f, 0.0f, -10.0f)); 

        YellowPlatformPosition = LerpRandomSpawnPosition(CurrentGameLevel, 0.25f, new Vector3(-10.0f, 0.0f, 20.0f), new Vector3(-10.0f, 0.0f, 10.0f), new Vector3(5.0f, 0.0f, 5.0f), new Vector3(10.0f, 0.0f, 20.0f));
        GreenPlatformPosition = LerpRandomSpawnPosition(CurrentGameLevel, 0.25f, new Vector3(-10.0f, 0.0f, -20.0f), new Vector3(-10.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(12.5f, 0.0f, -10.0f));

        RudiumPosition = YellowPlatformPosition + new Vector3(0.0f, 1.0f, 0.0f);

        //==============================================================================
        if (CurrentGameLevel > 10)
        {
            int RandomChoice = Random.Range(0, 5);
            CybermanStartPosition = PlayerStartPositions[RandomChoice].transform.position;
            RandomChoice = Random.Range(0, 5);
            DalekStartPosition = DalekStartPositions[RandomChoice].transform.position;
            RandomChoice = Random.Range(0, 5);
            YellowPlatformPosition = RudiumStartPositions[RandomChoice].transform.position;
            RandomChoice = Random.Range(0, 5);
            GreenPlatformPosition = RudiumGoalPositions[RandomChoice].transform.position;
            RudiumPosition = YellowPlatformPosition + new Vector3(0.0f, 1.0f, 0.0f);
        }

        // Now Assign the actual Game Objects Positions
        TheDalek.transform.position = DalekStartPosition;
        TheDalek.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        YellowPlatform.transform.position = YellowPlatformPosition;
        GreenPlatform.transform.position = GreenPlatformPosition;

        RudiumReactor.transform.position = RudiumPosition;
        RudiumRB.velocity = Vector3.zero;
        RudiumRB.angularVelocity = Vector3.zero;

        // Set the DalekLevel;
        TheDalek.SendMessage("ResetDalekEpisode", CurrentGameLevel);

        LevelTextDisplay.text = "Level: " + CurrentGameLevel.ToString();

        return CybermanStartPosition;
    } // ResetTheEpisode
    //==========================================================================
    // Update is called once per frame
    void Update()
    {

    }  // Update
    //==========================================================================
    Vector3 RandomDalekSpawnNorth(Vector3 WP1, Vector3 WP2)
    {
        Vector3 RtnSpawnPos = Vector3.zero;
        // Assume Game level 1..10
        float RandomExtent = (Random.Range(0.0f, 100.0f));
        Vector3 RandomChoicePosition = WP1;
        if (RandomExtent > 50.0f) RandomChoicePosition = WP1;
        else RandomChoicePosition = WP2;

        RtnSpawnPos = RandomChoicePosition + new Vector3(0.0f, 0.75f, 0.0f) + this.transform.position;

        return RtnSpawnPos;
    } // RandomDalekSpawnNorth
    // ===================================================================================
    // 
    Vector3 LerpRandomSpawnPosition(int GameLevel, float SpawnHeight, Vector3 V1, Vector3 V2, Vector3 V3, Vector3 V4)
    {
        Vector3 RtnSpawnPos = Vector3.zero;
        // Assume Game level 1..10
        float RandomExtent = (GameLevel * (Random.Range(0.0f, 100.0f) - 50.0f)) / 250.0f;   // Should Range between  -2.0, 2.0
        Vector3 LerpedPosition = V2;
        if (RandomExtent < -1.0f) LerpedPosition = Vector3.Lerp(V1, V2, (RandomExtent + 2.0f));
        if ((RandomExtent > -1.0f) && (RandomExtent < 1.0f)) LerpedPosition = Vector3.Lerp(V2, V3, (RandomExtent + 1.0f)/2.0f);
        if (RandomExtent > 1.0f) LerpedPosition = Vector3.Lerp(V3, V4, (RandomExtent - 1.0f));
        RtnSpawnPos = LerpedPosition + new Vector3(0.0f, SpawnHeight, 0.0f) +  this.transform.position; 
        
        return RtnSpawnPos;
    } // LerpRandomSpawnPosition
    // =========================================================================
    void HideAllSpawnObjects()
    {
        for (int I = 0; I < PlayerStartPositions.Length; I++) HideAnObject(PlayerStartPositions[I]);
        for (int I = 0; I < DalekStartPositions.Length; I++) HideAnObject(DalekStartPositions[I]);
        for (int I = 0; I < RudiumStartPositions.Length; I++) HideAnObject(RudiumStartPositions[I]);
        for (int I = 0; I < RudiumGoalPositions.Length; I++) HideAnObject(RudiumGoalPositions[I]);
    } // HideAllSpawnObjects
    //==========================================================================
    void HideAnObject(GameObject TheGO)
    {
        Renderer[] TheChildRenderers = TheGO.GetComponentsInChildren<Renderer>();
        foreach (Renderer AChildRenderer in TheChildRenderers)
        {
            AChildRenderer.enabled = false;
        }
    } // HideAnObject
    // =========================================================================
    void UpdateScoreDisplay()
    {
        ScoreTextDisplay.text = "CyberMan: " + CyberManScore.ToString() + " Dalek: " + DalekScore.ToString();
    }
    //==========================================================================
    public void IncrementDalekScore()
    {
        DalekScore = DalekScore + 1;
        UpdateScoreDisplay();
    }
    public void IncrementCyberManScore()
    {
        CyberManScore = CyberManScore + 1;
        UpdateScoreDisplay();
    }
    public void UpdateNarrativeString(string NewDisplayString)
    {
        NarrativeString = NewDisplayString;
        NarrativeTextDisplay.text = NarrativeString;
    }
    //==========================================================================
}
