using System.Collections;
using System.Collections.Generic;
using UnityEngine;using UnityEngine.AI;

public class DalekContrtoller : MonoBehaviour
{

    // ==================================================================
    // Start is called before the first frame update
    // ==================================================================
    // Note the Dalek Navigates around a NavMesh based environment
    // Use of Ray Casts and dot products to detect if the Dalek can see the player, and is in front view
    // see https://answers.unity.com/questions/438175/enemy-ai-detect-player-when-seen.html
    //

    NavMeshAgent TheNavMeshAgent;
    public GameObject TheGameManager;                  // Just to get Relative coordinates for Random Positions 

    // Dalek can see Player Variables
    float MaxSeeDistance = 12.5f;
    float LaserDistance = 7.5f; 
    Vector3 DalekHeadHeight;

    public GameObject TargetPlayer;

    public GameObject LaserRayContrtollerGO;

    public bool DalekCanSeePlayer; 

    private LaserGunController TheLaserGunController;
    private int GameLevel;

    // ===============================================================================================
    private void Awake()
    {
        TheNavMeshAgent = GetComponent<NavMeshAgent>();
        TheLaserGunController = LaserRayContrtollerGO.GetComponent<LaserGunController>();
    }  // Awake 
    // ==============================================================================================
    void Start()
    {
        // Initialise Towards the Centre of the Arena
        TheNavMeshAgent.SetDestination(TheGameManager.transform.position + new Vector3(0.0f, 0.75f, 0.0f));

        GameLevel = 1;
        DalekCanSeePlayer = false; 

    } // Start
    // ==================================================================
    public void ResetDalekEpisode(int ReqGameLevel)
    {
        GameLevel = ReqGameLevel;

        if (GameLevel < 3) TheNavMeshAgent.speed = 0.0f;
        if (GameLevel == 3) TheNavMeshAgent.speed = 0.1f; // 0.05f;
        if (GameLevel == 4) TheNavMeshAgent.speed = 0.2f;   // 0.1f;
        if (GameLevel == 5) TheNavMeshAgent.speed = 0.25f;  // 0.15f;
        if (GameLevel == 6) TheNavMeshAgent.speed = 0.275f; //  0.2f; 
        if (GameLevel == 7) TheNavMeshAgent.speed = 0.3f;  //  0.25f;     // Reprofile as transition between 7 and 8 into 0.3f was a signficant challenge
        if (GameLevel == 8) TheNavMeshAgent.speed = 0.35f; //0.3f;
        if (GameLevel == 9) TheNavMeshAgent.speed = 0.4f;     
        if (GameLevel == 10) TheNavMeshAgent.speed = 0.45f;   // 0.5f Was the prvious highest challenge
        
        if(GameLevel == 11) TheNavMeshAgent.speed = 0.4f;   // Retract Speed, as more diverse Spawns
        if(GameLevel == 12) TheNavMeshAgent.speed = 0.45f;
        if(GameLevel == 13) TheNavMeshAgent.speed = 0.5f;
        if(GameLevel == 14) TheNavMeshAgent.speed = 0.6f;

        LaserDistance = 7.5f;
        if (GameLevel < 5)
        {
            LaserDistance = 7.5f;
            MaxSeeDistance = 12.5f;
        }
        if ((GameLevel >= 5) && (GameLevel <= 10))
        {
            LaserDistance = 10.0f;
            MaxSeeDistance = 17.5f;
        }
        if (GameLevel > 10)
        {
            LaserDistance = 10.0f;
            MaxSeeDistance = 20.0f;
        }
        
        DalekCanSeePlayer = false;

        // Alwyas Direct the Dalek Towards centre of Arena at Start of the Episode
        TheNavMeshAgent.SetDestination(TheGameManager.transform.position + new Vector3( 0.0f,0.75f,0.0f));

    } // ResetDalekEpisode
    // ==================================================================
    private void FixedUpdate()
    {
        TheLaserGunController.DisableRay();   // The Ray Should be disabled by Default 
        CheckIfCanSeePlayer();
        if (DalekCanSeePlayer)
        {
            PursuePlayer();
            if(Vector3.Distance(TargetPlayer.transform.position,transform.position)< LaserDistance)
            {
                // Fire the Laser at the Player
                TheLaserGunController.EnableRay();
                TargetPlayer.SendMessage("BeingShot");
            }
        } // Check Can See Player
        
        if(TheNavMeshAgent.remainingDistance<0.75f)
        {
            GotoRandomDestination();
        }
    } // Fixed Update
    // ==============================================================================
    void GotoRandomDestination()
    {
        Vector3 RandomDeltaPos = new Vector3(Random.Range(-40, 40), 1.0f, Random.Range(-40, 40));
        TheNavMeshAgent.SetDestination(TheGameManager.transform.position + RandomDeltaPos);

    } // SetNewDestination
    // ==================================================================
    void PursuePlayer()
    {
        TheNavMeshAgent.SetDestination(TargetPlayer.transform.position); 

    } // Pursue Player 
    // ==================================================================
    void CheckIfCanSeePlayer()
    {
        DalekCanSeePlayer = false; 
        DalekHeadHeight = transform.position + new Vector3(0.0f, 3.0f, 0.0f); 
        Vector3 PlayerDirection = (TargetPlayer.transform.position - transform.position).normalized;
        if (Vector3.Dot(PlayerDirection, transform.forward) > 0.65f)
        {
            // Then the Dalek is facing in the Direction of the Player
            // So Now do a Ray Cast to check there is no Obstrcle between the Two
            RaycastHit HitObject;
            if(Physics.Raycast(DalekHeadHeight, PlayerDirection, out HitObject, MaxSeeDistance))
            {
                // Need to ensure that all (mesh) parts of the Player GO are tagged as being The Player
                if (HitObject.collider.gameObject.tag == "Player")
                {
                    DalekCanSeePlayer = true; 
                   // Debug.Log(" Dalek can See Player");
                }
            }
        } 
    } // CheckIfCanSeePlayer
    // ==================================================================
    void OnCollisionEnter(Collision theCollision)
    {
        //Debug.Log(" Dalek Collided With: " + gameObject.tag); 

    }  // OnCollisionEnter
    // ==================================================================
}
