using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGunController : MonoBehaviour
{

    private LineRenderer TheLaserLineRenderer;
    public Transform ThePlayerTarget;
    public Transform TheDalekLaserGun; 
    private bool DisplayRay;
    // =====================================================================


    // =====================================================================
    // Start is called before the first frame update
    void Start()
    {
        TheLaserLineRenderer = GetComponent<LineRenderer>();
        DisplayRay = false;
    }
    // =====================================================================
    // Update is called once per frame
    void Update()
    {
        // Will need to continous Update the Line as th Dalek and Player move in space
        TheLaserLineRenderer.SetPosition(0, TheDalekLaserGun.transform.position);
        if (DisplayRay)
        {
            Vector3 PlayerChestheight = ThePlayerTarget.transform.position + new Vector3(0.0f, 3.0f, 0.0f);
            TheLaserLineRenderer.SetPosition(1, PlayerChestheight);
        }
        else TheLaserLineRenderer.SetPosition(1, TheDalekLaserGun.transform.position);

        TheLaserLineRenderer.positionCount = 2;
    }  // Update
    // =====================================================================
    public void EnableRay()
    {
        DisplayRay = true;
    }
    public void DisableRay()
    {
        DisplayRay = false;
    }
    // =====================================================================


    // =====================================================================
}
