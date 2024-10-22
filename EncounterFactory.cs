//-----------------------------------------------------------------------------
//    filename: EncounterFactory.cs
//   author(s): Alex Schumer
//last updated: 10.18.2024
//       class: GAM-4XX
//     project: Gambler's Fallacy
//       brief: Factory to create encounters for the player to face.
//-----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public static class EncounterFactory
{
  private static Dictionary<string, GameObject> prefabs;
  private static GameObject groupTriggerVolumePrefab;
  private static StreamWriter stream;

  /// <summary>
  /// Load in all the prefabs for the factory.
  /// </summary>
  private static void Intialize()
  {
    if(prefabs == null)
      LoadPrefabs();

    if (groupTriggerVolumePrefab == null)
      groupTriggerVolumePrefab = Resources.Load<GameObject>("Prefabs/TriggerVolume/TriggerVolume");

    if(stream == null)
    {
      string filepath = TelemetryUtils.GetTelemetryFilePath("TelemetryData/encounters_created.csv");
      
      if(File.Exists(filepath) == false)
      {
        stream = new StreamWriter(filepath, false);
        stream.WriteLine("date,time,version,level_name,encounter_name,x_position,y_position,z_position,enemy_1,enemy_2,enemy_3,enemy_4,enemy_5");
      }
      else
        stream = new StreamWriter(filepath, true);
    }
  }



  /// <summary>
  /// Creates a single enemy. Enemy can be added to a group.
  /// </summary>
  /// <param name="type"></param>
  /// <param name="position"></param>
  /// <returns></returns>
  private static GameObject CreateEnemy(string type, Vector3 position)
  {
    Intialize();

    if (prefabs.ContainsKey(type))
    {
      GameObject enemy = GameObject.Instantiate(prefabs[type], position, Quaternion.identity);

      if(enemy == null)
        Debug.Log(type + " NOT IN PREFABS.");

      return enemy;
    }
    else
    {
      Debug.LogError("Enemy type not found: " + type);
      return null;
    }
  }

  /// <summary>
  /// Creates a group of enemies. Enemies can be added to a group.
  /// </summary>
  /// <param name="enemyTypes"></param>
  /// <param name="position"></param>
  /// <param name="encounterName"></param>
  /// <param name="spawnBoundry"></param>
  /// <returns></returns>
  public static GameObject CreateEnemyGroup(List<string> enemyTypes, Vector3 position, float spawnBoundry, string levelName)
  {
    Intialize();

    string encounterName = CreateEncounterName(enemyTypes);

    GameObject group = new GameObject(encounterName);
    group.transform.position = position;

    GameObject[] enemies = new GameObject[enemyTypes.Count];


    for (int i = 0; i < enemyTypes.Count; i++)
    {
      //Randomize the position of each enemy in the group. Within the bounds set.
      float newX = Random.Range(position.x - spawnBoundry, position.x + spawnBoundry);
      float newZ = Random.Range(position.z - spawnBoundry, position.z + spawnBoundry);
      Vector3 newPos = new Vector3(newX, position.y, newZ);

      //Create a new enemy to be added to the group.
      GameObject enemy = CreateEnemy(enemyTypes[i], newPos);

      if (enemy != null)
      {
        enemy.transform.SetParent(group.transform);

        TriggerVolume enemyTriggerVolume = enemy.GetComponentInChildren<TriggerVolume>();

        if (enemyTriggerVolume != null)
          enemyTriggerVolume.gameObject.SetActive(false);

        enemies[i] = enemy;
      }
    }

    GameObject groupTrigger = GameObject.Instantiate(prefabs["GroupTriggerVolume"], group.transform);
    groupTrigger.GetComponentInChildren<TriggerVolume>().triggerName = encounterName;

    WriteToCSV(enemyTypes, encounterName, levelName, position);
    Shutdown();

    return group;
  }

  /// <summary>
  /// Creates the name of the encounter based on the number and type of enemies spawned.
  /// </summary>
  /// <param name="enemyTypes"></param>
  /// <returns></returns>
  private static string CreateEncounterName(List<string> enemyTypes)
  {
    string encounterName = "";

    List<string> modNames = new List<string>();
    for (int i = 0; i < enemyTypes.Count; ++i)
    {
      modNames.Add(enemyTypes[i].Replace("Enemy", ""));
    }

    encounterName += string.Join("", modNames);
    encounterName += "_" + enemyTypes.Count;

    return encounterName;
  }

  /// <summary>
  /// Writes the encounter to the csv file.
  /// </summary>
  /// <param name="types"></param>
  /// <param name="encounterName"></param>
  /// <param name="levelName"></param>
  /// <param name="position"></param>
  private static void WriteToCSV(List<string> types, string encounterName, string levelName, Vector3 position)
  {
    string version = IsBuild();

    string logEntry = $"{System.DateTime.Now.ToString("MM/dd/yyyy")}," +
                      $"{System.DateTime.Now.ToString("HH:mm:ss")}," +
                      $"{version}," +
                      $"{levelName},"+
                      $"{encounterName}," +
                      $"{position.x}," +
                      $"{position.y}," +
                      $"{position.z},";

    logEntry += string.Join(",", types);

    stream.WriteLine(logEntry);
  }

  /// <summary>
  /// Returns whether the encounter was created in the editory or the build.
  /// </summary>
  /// <returns></returns>
  private static string IsBuild()
  {
    if (Application.isEditor)
      return "Editor";
    else
      return "Build";
  }

  /// <summary>
  /// Loads all the enemy prefabs.
  /// </summary>
  private static void LoadPrefabs()
  {
    prefabs = new Dictionary<string, GameObject>
    {
      {"MeleeEnemy", Resources.Load<GameObject>("Prefabs/Enemies/BasicEnemies/PF_MeleeEnemy")},
      {"RangedEnemy", Resources.Load<GameObject>("Prefabs/Enemies/BasicEnemies/PF_RangedEnemy")},
      {"BombEnemy", Resources.Load<GameObject>("Prefabs/Enemies/BasicEnemies/PF_BombEnemy")},
      {"ChargeEnemy", Resources.Load<GameObject>("Prefabs/Enemies/BasicEnemies/PF_ChargeEnemy")},
      {"RayEnemy", Resources.Load<GameObject>("Prefabs/Enemies/BasicEnemies/PF_RayEnemy")},
      {"GroupTriggerVolume",  Resources.Load<GameObject>("Prefabs/TriggerVolume/TriggerVolume")}
    };
  }

  /// <summary>
  /// Shuts down and cleans up the factory.
  /// </summary>
  public static void Shutdown()
  {
    if(stream != null)
    {
      stream.Flush();
      stream.Close();
      stream = null;
    }
  }
}
