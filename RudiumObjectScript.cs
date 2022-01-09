using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RudiumObjectScript : MonoBehaviour
{
    // =====================================================================
    private bool CurrentlyBeingHeld;
    private Rigidbody RudiumRigidBody;
    private BoxCollider TheBoxCollider;

    // =====================================================================
    void Start()
    {
        CurrentlyBeingHeld = false;
        RudiumRigidBody = GetComponent<Rigidbody>();
        TheBoxCollider = GetComponent<BoxCollider>();

    } // Start
    // =====================================================================
    public void ResetRudiumPosition(Vector3 PlacedPosition)
    {
        TheBoxCollider.enabled = true;
        RudiumRigidBody.isKinematic = false;

        transform.position = PlacedPosition;


    } // ResetRudiumPosition
    // =====================================================================
    public void UpdateHeldPosition(Vector3 HeldPosition)
    {
        if (CurrentlyBeingHeld)
        {
            transform.position = HeldPosition;
        }
    } // UpdateHeldPosition
    // =====================================================================
    public void Reset()
    {
        CurrentlyBeingHeld = false;
        RudiumRigidBody.isKinematic = false;
        TheBoxCollider.enabled = true;
        RudiumRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities
        RudiumRigidBody.angularVelocity = Vector3.zero;

    } // Reset
    // ===================================================================
    public void PlaceDown(Vector3 PlantPosition)
    {
        transform.position = PlantPosition;
        CurrentlyBeingHeld = false;
        RudiumRigidBody.isKinematic = false;
        TheBoxCollider.enabled = true;
    } // PlaceDown
    // =====================================================================
    public void PickedUp()
    {
        CurrentlyBeingHeld = true;
        RudiumRigidBody.isKinematic = true;
        TheBoxCollider.enabled = false;
    } // PickedUp
    // =====================================================================


    // =====================================================================
}
