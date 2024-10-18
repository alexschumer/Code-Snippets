//-----------------------------------------------------------------------------
//    filename: PlaytimeTelemetry.cs
//   author(s): Alex Schumer
//last updated: 10.18.2024
//       class: GAM-4XX
//     project: Gambler's Fallacy
//       brief: Script to for the trigger volume prefab. 
//-----------------------------------------------------------------------------
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Rendering;
using UnityEngine;

public class PlaytimeTelemetry : MonoBehaviour
{
  private GameObject player;
  private DebugConsole console;
  private bool dataLoaded = false;
  private string username;
  private float playtime;
  private string date;
  private StreamWriter stream;
  private string time;

  void Start()
  {
    if(console == null)
      console = FindObjectOfType<DebugConsole>();

    if (player = null)
      player = GameObject.FindWithTag("Player");

    username = console.username;

    this.date = DateTime.Now.ToString("MM.dd.yyyy");
    this.time = DateTime.Now.ToString("HH:mm:ss");
    this.playtime = 0f;

    string filepath = TelemetryUtils.GetTelemetryFilePath("TelemetryData/play_test_duration.csv");

    if (File.Exists(filepath) == false)
    {
      stream = new StreamWriter(filepath, false);
      stream.WriteLine("username,date,time,session_duration");
    }
    else
      stream = new StreamWriter(filepath, true);
  }

  void Update()
  {
    username = console.username;
    IncrementPlayTime();
  }

  void OnApplicationQuit()
  {
    if (stream != null)
    {
      WriteToCSV();

      stream.Flush();
      stream.Close();
      stream = null;
    }
  }

  /// <summary>
  /// Increments the playtime for the specific user.
  /// </summary>
  public void IncrementPlayTime()
  {
    playtime += Time.deltaTime;
  }

  /// <summary>
  /// Write a line to CSV if data collection is on.
  /// </summary>
  private void WriteToCSV()
  {
    if (console.collectData == false) return;

    if(stream != null && playtime > 0f)
      stream.WriteLine($"{username},{date},{time},{playtime}");
  }
}
