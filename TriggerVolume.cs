//-----------------------------------------------------------------------------
//    filename: TriggerVolume.cs
//   author(s): Alex Schumer
//last updated: 10.18.2024
//       class: GAM-4XX
//     project: Gambler's Fallacy
//       brief: Script to for the trigger volume prefab. 
//-----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

#nullable disable

[DisallowMultipleComponent()]
[RequireComponent(typeof(Collider))]

public class TriggerVolume : MonoBehaviour
{
  public UnityEvent onEnterTrigger;
  public UnityEvent onExitTrigger;
  public UnityEvent onTriggerStay;
  private Collider triggerCollider;
  private bool alreadyEntered = false;

  [Header("Settings:")]
  public string triggerName = "";
  private bool oneShot = false;

  [Header("Gizmo Settings:")]
  public bool dispayVolume = true;
  public bool showOnlyWhileSelected = false;
  [Header("Filters:")]
  public GameObject specificTriggerObject;
  public LayerMask layersToDetect = -1;//Default to everything.
  public Color volumeColor = Color.green;

  void Start()
  {
    specificTriggerObject = null;

    //Unless the user specfies an object, the player will be default.
    if (triggerName == "")
      triggerName = gameObject.transform.root.name;

    GameObject root = gameObject.transform.root.gameObject;

    //Gets the first EnemyAI script in the root object.
    //Adjust the bounds of the trigger volume to match the sight
    //range of the enemy.
    EnemyAI ai = root.GetComponentInChildren<EnemyAI>();
    if (ai != null)
    {
      AdjustBoundsToSightRange(ai.sightRange);
    }
  }

  private void Awake()
  {
    triggerCollider = GetComponent<Collider>();
    triggerCollider.isTrigger = true;
  }

  private void OnTriggerEnter(Collider other)
  {
    //validate object
    if(oneShot == true && alreadyEntered == true)
      return;

    //If we have specified an object to trigger this,
    //check if it is the one that entered.
    if (specificTriggerObject != null
        && 
        other.gameObject != specificTriggerObject)
      return;

    if(specificTriggerObject == null
      &&
      layersToDetect != (layersToDetect | (1 << other.gameObject.layer)))
      return;
    //If the object is not in the layer mask, return. (This is a bitwise operation)

    if(other.GetComponent<PlayerMovement>() != null)
      other.GetComponent<PlayerMovement>().currentRoom = triggerName;

    Debug.Log(triggerName + " entered.");

    onEnterTrigger.Invoke();
    alreadyEntered = true;
  }

  private void OnTriggerExit(Collider other)
  {
    //validate object
    if (oneShot == true && alreadyEntered == true)
      return;
    //If we have specified an object to trigger this, check if it is the one that entered.
    if (specificTriggerObject != null && other.gameObject != specificTriggerObject)
      return;

    if (specificTriggerObject == null && layersToDetect != (layersToDetect | (1 << other.gameObject.layer)))
      return; //If the object is not in the layer mask, return. (This is a bitwise operation)

    Debug.Log(triggerName + " exit.");

    onEnterTrigger.Invoke();
    alreadyEntered = true;
  }

  private void OnTriggerStay(Collider other)
  {
    //validate object
    if (oneShot == true && alreadyEntered == true)
      return;
    //If we have specified an object to trigger this, check if it is the one that entered.
    if (specificTriggerObject != null && other.gameObject != specificTriggerObject)
      return;

    if (specificTriggerObject == null && layersToDetect != (layersToDetect | (1 << other.gameObject.layer)))
      return; //If the object is not in the layer mask, return. (This is a bitwise operation)

    onTriggerStay.Invoke();
    alreadyEntered = true;
  
  }

  private void OnDrawGizmos()
  {
    if (dispayVolume == false)
      return;
    if (showOnlyWhileSelected == true)
      return;

    if (triggerCollider == null)
      triggerCollider = GetComponent<Collider>();

    UpdateVolumeColor(volumeColor, 0.4f);
  }

  private void OnDrawGizmosSelected()
  {
    if (dispayVolume == false)
      return;
    if (showOnlyWhileSelected == false)
      return;

    if (triggerCollider == null)
      triggerCollider = GetComponent<Collider>();

    UpdateVolumeColor(volumeColor, 0.5f);
  }

  /// <summary>
  /// Updates the color of the trigger volume and draws the box.
  /// </summary>
  /// <param name="color"></param>
  /// <param name="alpha"></param>
  void UpdateVolumeColor(Color color, float alpha)
  {
    //Sets the color of the trigger box.
    Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
    Gizmos.DrawCube(transform.position, triggerCollider.bounds.size);
  }

  /// <summary>
  /// Resets the volume after it has been entered the first time.
  /// </summary>
  public void ResetTrigger()
  {
    alreadyEntered = false;
  }

  /// <summary>
  /// Adjusts the bounds of the box based on the sight range of the enemy.
  /// </summary>
  /// <param name="sightrange"></param>
  void AdjustBoundsToSightRange(float sightrange)
  {
    BoxCollider boxcollider = triggerCollider as BoxCollider;

    if(boxcollider != null)
    {
      boxcollider.size = new Vector3(sightrange, boxcollider.size.y * 3, sightrange);
    }
  }
}
