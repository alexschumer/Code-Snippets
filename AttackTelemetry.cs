//-----------------------------------------------------------------------------
//    filename: AttackTelemetry.cs
//   author(s): Alex Schumer
//last updated: 10/4/2024
//       class: GAM-4XX
//     project: Wild West Card Shooter.
//       brief: Telemetry script for the player character to collect data. 
//-----------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class AttackTelemetry : MonoBehaviour
{
  private string filename = "TelemetryData/player_attack_data.csv";
  private GameObject player;
  private PlayerStats playerStats;
  private Hand hand;
  private string currentRoom;

  //private variables
  private DebugConsole console;
  private PlayerAttack playerAttack;
  private PlayerMovement playerMovement;
  private StreamWriter stream;
  private Dictionary<Card.SuitType, int> cardTypes;
  private float runtime;
  private string date;

  void Start()
  {
    if (console == null)
      console = FindObjectOfType<DebugConsole>();

    date = DateTime.Now.ToString("MM.dd.yyyy");

    cardTypes = new Dictionary<Card.SuitType, int>();

    //Sets the player movement script to the playerMovement variable.
    if (player == null)
    {
      player = FindObjectOfType<PlayerMovement>().gameObject;
      playerMovement = player.GetComponent<PlayerMovement>();
      playerAttack = player.GetComponent<PlayerAttack>();
      playerStats = player.GetComponent<PlayerStats>();
      currentRoom = playerMovement.currentRoom;
      hand = player.GetComponent<Hand>();

      if (playerAttack != null)
        playerAttack.OnAttack += OnPlayerAttack;
    }

    filename = TelemetryUtils.GetTelemetryFilePath(filename);

    if (File.Exists(filename) == false)
    {
      stream = new StreamWriter(filename, false);
      WriteLine("username,date,time,room,runtime,none,blank,fire,slow,decay,iron,curse,gamblers_fallacy,electricity");
    }
    else
      stream = new StreamWriter(filename, true);

    LoadCardSuitTypes();
  }

  /// <summary>////////////////////////////////////////////////////////////////
  /// Begins to collect data when the trigger box is entered.
  /// </summary>///////////////////////////////////////////////////////////////
  public void Update()
  {
    IncrementTime();
    
    UpdateRoomName();
  }


  private void OnApplicationQuit()
  {
    if (stream != null)
    {
      stream.Flush();
      stream.Close();
      stream = null;
    }

    if (playerAttack != null)
      playerAttack.OnAttack -= OnPlayerAttack;
  }

  private void IncrementTime()
  {
    runtime += Time.deltaTime;
  }

  private void OnPlayerAttack()
  {
    UpdateCardType();
  }
  
  /// <summary>
  /// Loads all of the card type enums in a dictionary for telemetry use.
  /// </summary>
  private void LoadCardSuitTypes()
  {
    foreach (Card.SuitType suit in Enum.GetValues(typeof(Card.SuitType)))
    {
      cardTypes.Add(suit, 0);
    }
  }

  /// <summary>////////////////////////////////////////////////////////////////
  /// Updates the name of the room the player is in.
  /// </summary>///////////////////////////////////////////////////////////////
  /// <param name="newRoomName"></param>
  public void UpdateRoomName()
  {
    currentRoom = playerMovement.currentRoom;
  }

  

  /// <summary>////////////////////////////////////////////////////////////////
  /// Collects the player position.
  /// </summary>///////////////////////////////////////////////////////////////
  private void WriteCardTypeToCSV()
  {
    if (console.collectData == false) return;

    // Now including the room name in the logged data
    WriteLine(
              $"{console.username}" +
              $",{date}" +
              $",{DateTime.Now.ToString("HH:mm:ss")}" +
              $",{currentRoom}" +
              $",{runtime}"
              + "," + cardTypes[Card.SuitType.None] 
              + "," + cardTypes[Card.SuitType.Blank]
              + "," + cardTypes[Card.SuitType.Fire]
              + "," + cardTypes[Card.SuitType.Slow]
              + "," + cardTypes[Card.SuitType.Decay]
              + "," + cardTypes[Card.SuitType.Iron]
              + "," + cardTypes[Card.SuitType.Curse]
              + "," + cardTypes[Card.SuitType.GamblersFallacy]
              + "," + cardTypes[Card.SuitType.Electricity]
              );
  }

  /// <summary>////////////////////////////////////////////////////////////////
  /// Adds a line to the csv file.
  /// </summary>///////////////////////////////////////////////////////////////
  /// <param name="line"></param>
  public void WriteLine(string line)
  {
    if(stream != null)
      stream.WriteLine(line);
  }

  public void UpdateCardType()
  {
    Card currentCard = hand.GetComponent<Hand>().CurrentCard();

    if (currentCard == null)
    {
      Debug.LogWarning("NULL refrence to current card.");
      currentCard.Suit = Card.SuitType.None;
    }

    if(cardTypes.ContainsKey(currentCard.Suit))
    {
      cardTypes[currentCard.Suit]++;
    }

    WriteCardTypeToCSV();
  }
}

/*
 LOG:
10/4/2024: Revamed everything back over to MonoBehavior - Alex Schumer

 */
