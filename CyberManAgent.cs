using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CyberManAgent :Agent
{
    public enum PlayerState { Idle, Walking, PickingUp, WalkingWith, WaitingWith, PlacingDown, Dying };
    // =============================================================

    public GameObject TheGameManager;
    public GameObject TheRudiumReactor;
    public GameObject TheGoalArea;
    public GameObject TheDalek;
    private GameControl TheGameControlScript;

    private CharacterController TheCharController;

    private float WalkSpeed = 3.25f;
    private float gravity = -9.8f;
    private float PlayerRotationRate = 100.0f;
    bool CurrentlyGrounded;

    private PlayerState ThePlayerCurrentState;
    private Animator ThePlayerAnimator;
    private string CurrentAnimationName;

    private bool IsAbleToPickup;
    private bool CurrentlyHasTheProbe;
    private float DistanceToProbe;
    private float PickupProbeThreshold = 3.75f;
    private DalekContrtoller TheDalekController;

    private int CurrentGameLevel;

    private float CurrentDalekDistance; 
    private float PrevDalekDistance;

    private int EpisodeStepCount = 0;
    private bool PreviouslySeen;
    private int MaxStepCount = 10000;
    private bool FirstProbePickup;
    private bool CompletingSuccesfulRudiumPlacement; 
    // =============================================================
    private void Awake()
    {
        if (GetComponent<CharacterController>() != null) TheCharController = GetComponent<CharacterController>();
        else Debug.Log("*** ERROR: Player Cannot Get Its Character Controller");

        ThePlayerAnimator = GetComponentInChildren<Animator>();
        if (ThePlayerAnimator == null) Debug.Log("*** ERROR: Player Could Not Get Its Child Animator");

        TheGameControlScript = TheGameManager.GetComponent<GameControl>();
        if (TheGameControlScript == null) Debug.Log("*** ERROR: Player Cannot Get Game Manager Script");

        TheDalekController = TheDalek.GetComponent<DalekContrtoller>();
        if (TheDalekController == null) Debug.Log("*** ERROR: Player Cannot Get Dalek Script");

    } // Awake 
      // =============================================================
    public override void Initialize()
    {
        TheCharController.enabled = true;

        CurrentGameLevel = 1;

        float LessonGameLevelF = Academy.Instance.EnvironmentParameters.GetWithDefault("Gamelevel", 1.0f);
        CurrentGameLevel = (int)Mathf.CeilToInt(LessonGameLevelF);
        if (CurrentGameLevel < 1) CurrentGameLevel = 1;

        TheGameControlScript.UpdateNarrativeString("Have a Great Game");

    } // Initialize
      // ======================================================================================================================
    public override void OnEpisodeBegin()
    {
        float LessonGameLevelF = Academy.Instance.EnvironmentParameters.GetWithDefault("Gamelevel", 1.0f);
        CurrentGameLevel = (int)Mathf.CeilToInt(LessonGameLevelF);
        if (CurrentGameLevel < 1) CurrentGameLevel = 1; 

        transform.position = TheGameControlScript.ResetTheEpisode(CurrentGameLevel);
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
  
        SetPlayerIdle();
        EpisodeStepCount = 0;

        // Need to Reset the Probe State Colliders etc (As may have ended previous episode, whilst Holding, Colliders Off)  
        TheRudiumReactor.SendMessage("Reset");

        IsAbleToPickup = false;
        CurrentlyHasTheProbe = false;
        PreviouslySeen = false;
        FirstProbePickup = false;
        CompletingSuccesfulRudiumPlacement = false;

        CurrentDalekDistance = Vector3.Distance(TheDalek.transform.position, transform.position);
        PrevDalekDistance = CurrentDalekDistance; 

    } // OnEpisodeBegin()
    // =====================================================================================================================
    public override void CollectObservations(VectorSensor sensor)
    {
        // Add Explicit Relative Angles and Distances to various Game Objects

        sensor.AddObservation(DalekDirectionToPlayer());        // Dalek Direction Towards Player

        // [**CHEAT**] Add Back in Dalek to Help Aid Perception of Dalek
        sensor.AddObservation(AngleRelativeToPlayerHeading(TheDalek));
        sensor.AddObservation(Vector3.Distance(TheDalek.transform.position, transform.position) / 50.0f);

        // Add Delta Dalek Delta Closing Distances in lieu of Velocity and Instead of using any Stacked Frames
        CurrentDalekDistance = Vector3.Distance(TheDalek.transform.position, transform.position);
        float ClosingDeltaDistance = (PrevDalekDistance - CurrentDalekDistance)*20.0f;
        sensor.AddObservation(ClosingDeltaDistance);
        PrevDalekDistance = CurrentDalekDistance;

        // [**CHEAT**] Relative Direction and Distance to Goal Area and Probe
        sensor.AddObservation(AngleRelativeToPlayerHeading(TheGoalArea));
        sensor.AddObservation(Vector3.Distance(TheGoalArea.transform.position, transform.position) / 50.0f);
        sensor.AddObservation(AngleRelativeToPlayerHeading(TheRudiumReactor));
        sensor.AddObservation(Vector3.Distance(TheRudiumReactor.transform.position, transform.position) / 50.0f);

        // [**CHEAT**] Add Back in Relative Vector to Probe and Goal  - Even if Cannot see 
        //sensor.AddObservation((TheGoalArea.transform.position.x - transform.position.x) / 50.0f);   // Goal.x - Player.x
        //sensor.AddObservation((TheGoalArea.transform.position.z - transform.position.z) / 50.0f);   // Goal.z - Player.z
        //sensor.AddObservation((TheRudiumReactor.transform.position.x - transform.position.x) / 50.0f);   // Rudium.x - Player.x
        //sensor.AddObservation((TheRudiumReactor.transform.position.z - transform.position.z) / 50.0f);   // Rudium.z - Player.z

        // Add in the Boolean State Indicators 
        sensor.AddObservation(CurrentlyHasTheProbe);            
        sensor.AddObservation(IsAbleToPickup);
        sensor.AddObservation(TheDalekController.DalekCanSeePlayer);

        // So a Total of 11x floats explicit Observations (+ the Ray casts) 

    }// CollectObservations
     // ==========================================================================================================
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Called to Apply An Action Step - In Liue of Fixed Update Processing 
        EpisodeStepCount++;

        // Clear Down the Game Narrative
        if (EpisodeStepCount==500) TheGameControlScript.UpdateNarrativeString("");

        // Check if Excess Number of Steps
        if (EpisodeStepCount > MaxStepCount)
        {
            // Excessive Episode Step Count  - Kill off the Player
            if (ThePlayerCurrentState != PlayerState.Dying)
            {
                SetReward(-0.75f);    // Bad but not as bad as being hit by Dalek
                SetPlayerDeath();
            }
        }  // Excess Episode Length
        // ==================================================================================

        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;

        // Apply Rotation Actions
        if (actionBuffers.DiscreteActions[0] == 2) transform.Rotate(new Vector3(0.0f, PlayerRotationRate * Time.deltaTime, 0.0f), Space.Self);  // Rotate Negative Action
        if (actionBuffers.DiscreteActions[0] == 3) transform.Rotate(new Vector3(0.0f, -PlayerRotationRate * Time.deltaTime, 0.0f), Space.Self);     // Rotate Positive Action

        // ==========================================
        // Player Forward Movement Control Actions
        Vector3 DeltaLocalMovement = Vector3.zero;
        // Check If Forward Motion being Requested 
        if (actionBuffers.DiscreteActions[0] == 1)
        {
            // Need to Start Moving, if currently Just Idle
            if (ThePlayerCurrentState == PlayerState.Idle) SetPlayerWalking();
            if (ThePlayerCurrentState == PlayerState.WaitingWith) SetPlayerWalkingWith();

            // Only Move the Player if Actually Walking  as a fn if Various Walking Annimations
            if ((CurrentAnimationName == "CyberMan_Walk") || (CurrentAnimationName == "CyberMan_WalkWith"))
            {
                DeltaLocalMovement = transform.forward * WalkSpeed;
            }
        }
        else
        {
            // Assumes actionBuffers.DiscreteActions[0] == 0, so NOOP on Forward Direction - So Set Idle or Waiting idle
            if ((ThePlayerCurrentState == PlayerState.Walking) || (ThePlayerCurrentState == PlayerState.WalkingWith))
            {
                // No Action Being Requested so Set to Idle or Waiting
                if (CurrentAnimationName == "CyberMan_WalkWith") SetPlayerWaitingWith();
                if (CurrentAnimationName == "CyberMan_Walk") SetPlayerIdle();
            }
        } // If/Else on Forward Motion Action
        PerformDeltaMovement(DeltaLocalMovement);
        // =====================================
        // Check if Player Has the Probe
        if(CurrentlyHasTheProbe)
        {
            Vector3 ProbeHeldPosition = transform.position + transform.forward * 1.0f + new Vector3(0.0f, 1.5f, 0.0f);
            TheRudiumReactor.SendMessage("UpdateHeldPosition", ProbeHeldPosition);
        }
        else
        {
            DistanceToProbe = Vector3.Distance(transform.position, TheRudiumReactor.transform.position);
        }
        // =============================================
        // Step Penalty  (Max of -0.5f) 
        if((ThePlayerCurrentState == PlayerState.Idle) || (ThePlayerCurrentState == PlayerState.WaitingWith)) AddReward(-0.5f / MaxStepCount);
        if((ThePlayerCurrentState == PlayerState.Walking) || (ThePlayerCurrentState == PlayerState.WalkingWith)) AddReward(-0.25f / MaxStepCount);
        if (TheDalekController.DalekCanSeePlayer) AddReward(-0.25f / MaxStepCount);
        
        // ===============================
        // Check Able to Pickup Probe
        CheckAbleToPickup();

        // Check If Requesting a  T - Pickup Action
        if ((actionBuffers.DiscreteActions[0] == 4) && (IsAbleToPickup))
        {
            ThePlayerCurrentState = PlayerState.PickingUp;
            SetPlayerPickingUp();          // Pickup Action

            if (FirstProbePickup == false)
            {
                AddReward(0.2f);               // Add a Small reward for the Pickup 
                FirstProbePickup = true;
            }
        }  // Pickup Action

        // Check If Requesting a  P - PlaceDown Action
        if ((actionBuffers.DiscreteActions[0] == 5) && (CurrentlyHasTheProbe))
        {
            if (((ThePlayerCurrentState == PlayerState.WalkingWith) && (CurrentAnimationName == "CyberMan_WalkWith")) || ((ThePlayerCurrentState == PlayerState.WaitingWith) && (CurrentAnimationName == "CyberMan_WaitWith")))
            {
                ThePlayerCurrentState = PlayerState.PlacingDown;
                AddReward(-0.2f);
                SetPlayerPuttingDown();          // Placing Down Action
                CheckPlantedProbeAtGoal();   // This will Assign the +2.0 Reward 
            }
        } // Place Down Action
        // =========================================================
        // Now Check the Annimation States
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnimationName == "CyberMan_PickUp") && (ThePlayerCurrentState == PlayerState.PickingUp))
        {
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.25f)
            {
                CurrentlyHasTheProbe = true;
                TheRudiumReactor.SendMessage("PickedUp");
            }
            // Only Change Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                SetPlayerWalkingWith();
            }  // 100 % Complte
        }  // Pickup Animation Progress Check 
        // =================================
        // Check Animations Progress
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnimationName == "CyberMan_PutDown") && (ThePlayerCurrentState == PlayerState.PlacingDown))
        {
            // Plant the Ball at 10% Animaiton 
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.1f)
            {
                Vector3 ProbePlantedPosition = transform.position + transform.forward * 1.25f + new Vector3(0.0f, 0.25f, 0.0f);
                TheRudiumReactor.SendMessage("PlaceDown", ProbePlantedPosition);
                CurrentlyHasTheProbe = false;
            }  // 10% Through Plant Annimation
            // Only Check New Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f)
            {
                SetPlayerWalking();
                CheckCompletePlantedProbeAtGoal();   // Will End Episode
            }  // Plant Animation Completed

        }  // Planting Ball Animation Progres Check 
        // =================================
        // If Dying (Either out of Steps or has been killed by Dalek
        if ((CurrentAnimationName == "CyberMan_Death") && (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f) && (ThePlayerCurrentState== PlayerState.Dying))
        {
            if(EpisodeStepCount >= MaxStepCount)
            {
                TheGameControlScript.UpdateNarrativeString("Episode Timed Out !");
            }
            else
            {
                // Death Due to Dalek 
                TheGameControlScript.UpdateNarrativeString("CyberMan Was Killed !");
                TheGameControlScript.IncrementDalekScore();
            }
            EndEpisode();
        }  // End of Death Scene
        // =========================================================
        // Check Being Seen by Dalek
        if ((TheDalekController.DalekCanSeePlayer) && (!PreviouslySeen) && (ThePlayerCurrentState != PlayerState.Dying))
        {
            // Dalek Can Now see player
            TheGameControlScript.UpdateNarrativeString("I Can See You !");
            PreviouslySeen = true;
        }
        if ((!TheDalekController.DalekCanSeePlayer) && (PreviouslySeen) && (ThePlayerCurrentState != PlayerState.Dying))
        {
            // Dalek No Longer Seeing player
            TheGameControlScript.UpdateNarrativeString("");
            PreviouslySeen = false;
        }
        // ==================================================================
        // Check that Rudium Probe still in Game
        if(TheRudiumReactor.transform.position.y<-2.0f)
        {
            // Fallen Belo Platform so End Episode  - By Killing Off the Player
            BeingShot(); 
        }

    } // OnActionReceived
    // ===================================================================================================
    public override void Heuristic(in ActionBuffers actionsOut)
    // Hueristic Manual Actions
    {
        // Capture and Perform the Manual User Controls
        // Branch 0: forward Motion Actions: discreteActionsOut[0] = 0:NOOP, 1: Foward
        // Branch 1: Rotatation Actions: discreteActionsOut[1] = 0:NOOP,  1: Rotate Left, 2: Rotate Right,  
        // Branch 2: Play Actions : discreteActionsOut[1] = 0:NOOP, 1: Pickup, 2: Place Down

        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;      // 0:NOOP, 1: Forward Action
       // discreteActionsOut[1] = 0;      // 0:NOOP 1: Rotate Left, 2: Rotate Right Actions
       // discreteActionsOut[2] = 0;      // 0:NOOP, 1: Pickup, 2: Place Down Actions

        // Keyboard Motion Actions 
        if (Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[0] = 1;       // Move Forward
        if (Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[0] = 2;    // Rotate Left
        if (Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[0] = 3;     // Rotate Right

        // Keyboard Play Actions 
        if ((Input.GetKey(KeyCode.T)) && (IsAbleToPickup)) discreteActionsOut[0] = 4;         // Pickup Action
        if ((Input.GetKey(KeyCode.P)) && (CurrentlyHasTheProbe)) discreteActionsOut[0] = 5;   // Place Down Actions 

        // Debug the Game levels Spawn
        if (Input.GetKey(KeyCode.Q)) EndEpisode(); 

    } // Heuristic Manula Actions
    // ================================================================================================================
    void PerformDeltaMovement(Vector3 TheDeltaMovement)
    {
        // May need a better Grounded Function, Ray cast Down Height Calculation
        TheDeltaMovement.y = 0.0f;    // ** Try to Avoid Sky Walking !
        if (!CurrentlyGrounded)
        {
            TheDeltaMovement.y = 100.0f * gravity;
        }
        TheDeltaMovement = TheDeltaMovement * Time.deltaTime;
        // Now Perform the actual Character Contoller Movement    
        TheCharController.Move(TheDeltaMovement);

    }// PerformDeltaMovement
    // =============================================================
    float DalekDirectionToPlayer()
    {
        float ADirectionDot = 0.0f;

        Vector3 DaleKToPlayer = (transform.position - TheDalek.transform.position).normalized;
        ADirectionDot = Vector3.Dot(TheDalek.transform.forward, DaleKToPlayer);

        return ADirectionDot;
    } // DalekDirectionToPlayer
    // =============================================================
    float AngleRelativeToPlayerHeading(GameObject TheTargetObject)
    {
        float RelBearing = -1.0f;
        // from https://docs.unity3d.com/ScriptReference/Vector3.SignedAngle.html
        RelBearing = Vector3.SignedAngle(TheTargetObject.transform.position - transform.position, transform.forward, transform.up)/180.0f; 

        return RelBearing; 
    }  // RelativeBearingToPlayer
    // =============================================================
    // Check to If able to Pickup 
    void CheckAbleToPickup()
    {
        IsAbleToPickup = false;
        CurrentAnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;

        if (((ThePlayerCurrentState == PlayerState.Walking) && (CurrentAnimationName == "CyberMan_Walk")) || ((ThePlayerCurrentState == PlayerState.Idle) && (CurrentAnimationName == "CyberMan_Idle")))
        {
            // Check thta Neither Player Has the Probe
            if (!CurrentlyHasTheProbe)
            {
                // Requesting Picking Up the Probe 
                if ((DistanceToProbe < PickupProbeThreshold))
                {
                    // Take Probe if its In Front of Player
                    Vector3 DirectionToProbe = (TheRudiumReactor.transform.position - transform.position).normalized;
                    if (Vector3.Dot(transform.forward, DirectionToProbe) > 0.4f)
                    {
                        IsAbleToPickup = true;
                    }
                }
            } // Not Holding the Probe
        } // Only Pickup whilst Walking
    } // CheckAbleToPickup
    // =============================================================
    void CheckPlantedProbeAtGoal()
    {
        // Called Always upon Placing Probe
        float DistanceBetweenProbeAndGoal = Vector3.Distance(TheGoalArea.transform.position, TheRudiumReactor.transform.position);
        if ((DistanceBetweenProbeAndGoal < PickupProbeThreshold * 1.25f)  && (!CompletingSuccesfulRudiumPlacement))
        {
            AddReward(1.25f);    //  Max Success - Mission Complete 
            CompletingSuccesfulRudiumPlacement = true; 
        }  // Rudium within Goal
    } // CheckPlantedProbeAtGoal
      // =============================================================
    void CheckCompletePlantedProbeAtGoal()
    {
        // Called at 100% Animation Completion 
        float DistanceBetweenProbeAndGoal = Vector3.Distance(TheGoalArea.transform.position, TheRudiumReactor.transform.position);
        if (DistanceBetweenProbeAndGoal < PickupProbeThreshold * 1.25f)
        {
            // Probe has been Dropped within to Goal Area
            TheGameControlScript.UpdateNarrativeString("Rudium Delivered !  in: " + EpisodeStepCount.ToString());
            TheGameControlScript.IncrementCyberManScore();

            CompletingSuccesfulRudiumPlacement = false;
            EndEpisode();
        } // Rudium within Goal
    } // CheckPlantedProbeAtGoal
    // =============================================================
    public void BeingShot()
    {
        if (ThePlayerCurrentState != PlayerState.Dying)
        {
            TheGameControlScript.UpdateNarrativeString(" Arrgh!! ");
            AddReward(-1.0f);   // Max Negative Reward Death 
            SetPlayerDeath();
        }
    } // BeingShot
    // =============================================================
    void OnCollisionEnter(Collision theCollision)
    {
        if (theCollision.gameObject.tag == "Floor") CurrentlyGrounded = true;
    }  // OnCollisionEnter
    // =====================================
    void OnCollisionExit(Collision theCollision)
    {
        if (theCollision.gameObject.name == "floor") CurrentlyGrounded = false;
    } // OnCollisionExit
    // =============================================================
    // ========= The Annimation Control States =====================
    // {Idle, Walking, PickingUp, WalkingWith, WaitingWith, PlacingDown, Dying};
    void SetPlayerIdle()
    {
        ThePlayerCurrentState = PlayerState.Idle;
        ThePlayerAnimator.SetBool("IsWalking", false);
        ThePlayerAnimator.SetBool("CurrentlyHasTheProbe", false);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsPuttingDown", false);
        ThePlayerAnimator.SetBool("BeingKilled", false);
    } // SetPlayerIdleState
    // ==================================================================
    void SetPlayerWalking()
    {
        ThePlayerCurrentState = PlayerState.Walking;
        ThePlayerAnimator.SetBool("IsWalking", true);
        ThePlayerAnimator.SetBool("CurrentlyHasTheProbe", false);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsPuttingDown", false);
        ThePlayerAnimator.SetBool("BeingKilled", false);
    } // SetPlayerWalkingState
    // ==================================================================
    void SetPlayerWalkingWith()
    {

        ThePlayerCurrentState = PlayerState.WalkingWith;
        ThePlayerAnimator.SetBool("IsWalking", true);
        ThePlayerAnimator.SetBool("CurrentlyHasTheProbe", true);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsPuttingDown", false);
        ThePlayerAnimator.SetBool("BeingKilled", false);
    } // SetPlayerWalkingWithState
    // ==================================================================
    void SetPlayerPickingUp()
    {
        ThePlayerCurrentState = PlayerState.PickingUp;
        ThePlayerAnimator.SetBool("IsWalking", false);
        ThePlayerAnimator.SetBool("CurrentlyHasTheProbe", false);

        ThePlayerAnimator.SetBool("IsPickingUp", true);
        ThePlayerAnimator.SetBool("IsPuttingDown", false);
        ThePlayerAnimator.SetBool("BeingKilled", false);
    } // SetPlayerPickingUpState
    // ==================================================================
    void SetPlayerWaitingWith()
    {
        ThePlayerCurrentState = PlayerState.WaitingWith;
        ThePlayerAnimator.SetBool("IsWalking", false);

        ThePlayerAnimator.SetBool("CurrentlyHasTheProbe", true);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsPuttingDown", false);
        ThePlayerAnimator.SetBool("BeingKilled", false);
    } // SetPlayerWaitingState
    // ==================================================================
    void SetPlayerPuttingDown()
    {
        ThePlayerCurrentState = PlayerState.PlacingDown;

        ThePlayerAnimator.SetBool("IsWalking", false);

        ThePlayerAnimator.SetBool("CurrentlyHasTheProbe", true);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsPuttingDown", true);
        ThePlayerAnimator.SetBool("BeingKilled", false);
    } // SetPlayerOuttingDownState
      // ==================================================================
    void SetPlayerDeath()
    {
        ThePlayerCurrentState = PlayerState.Dying;

        ThePlayerAnimator.SetBool("BeingKilled", true);
    } // SetPlayerDeathState
    // ======================================================================================================================
}
