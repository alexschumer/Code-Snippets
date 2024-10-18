/******************************************************************************
 * filename: ActionList.cs
 * author:   Alex Schumer
 * class:    DES-315
 * project:  Card Game Proptotype
 * brief: This file contains the action list object.
 *****************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using TMPro;
using System.Net;
using System.Reflection;


public class ActionList : MonoBehaviour
{ 
  public List<Action> actions;
  public List<Action> pauseactions;
  public List<GameObject> pot;
  public Table table;
  public BetButton betbutton;
  public Telemetry data;
  Player winningplayer;
  public GameObject pause;
  public Log log;
  public Canvas graphicscanvas;

  public float gamedt;
  public float pausedt;
  public bool blocking;
  public bool bettriggered = false;
  public bool foldtriggered = false;
  public int numberofcoinsperbet;
  public bool playactive;
  public int betamount = 0;
  bool automode = false;
  public float gamespeed;
  public bool styleflag;
  string summary;
  bool logflag;

  //Called before the start of the first frame.
  void Start()
  {
    //variables
    gamedt = pausedt = Time.deltaTime;
    playactive = true;
    styleflag = true;
    logflag = false;

    //sets
    actions = new List<Action>();
    pauseactions = new List<Action>();
    table = GetComponent<Table>();
    betbutton = GetComponentInChildren<BetButton>();
  }

  //Called on every frame.
  void Update()
  {
    if (GameOver())
    {
      actions.RemoveRange(0, actions.Count);
      data.CloseStream();

      table.PauseMenu();
      
      RunActions();

      gamedt = 0.0f;
      pausedt = 0.0f;
    }
    else
    {
      if (logflag == true && actions.Count > 0)
        log.HandleLog("Action Count: " + actions.Count.ToString(), "", LogType.Exception);

      if (table.pauseflag == true)
      {
        gamedt = 0.0f;
      }
      else
      {
        if (automode)
        {
          gamedt = pausedt = Time.deltaTime * gamespeed;

          if (IsEmpty())
          {
            AutoModePauseMenu();
            playactive = true;
            bettriggered = true;

            pause.GetComponentInChildren<PauseMenu>().ResumeGame();
          }
        }
        else
          gamedt = pausedt = Time.deltaTime;
      }
      RunActions();
      AdjustNumberOfPlayers();
      SetPlayerPlayStyle();
      DealCardsNewRound();

      if (Input.GetKeyDown(KeyCode.D))
        logflag = !logflag;

      if (Input.GetKeyDown(KeyCode.A))
      {
        automode = !automode;
      }

      if (bettriggered)
      {
        BetAction(table.players[0], 0.0f);
      }

      if (foldtriggered)
      {
        FoldAction(table.players[0], 0.0f);
      }

      if (bettriggered || foldtriggered)
      {
        for (int i = 1; i < table.players.Count; ++i)
        {
          try
          {
            if (table.players[i].bettrigger)
            {
              BetAction(table.players[i], i);
              table.players[i].bettrigger = false;
            }
            else if (table.players[i].foldtrigger)
            {
              FoldAction(table.players[i], i);
              table.players[i].foldtrigger = false;
            }
          }
          catch (Exception e)
          {
            Debug.Log(e);
          }
        }
      }

      ResetButtonFlags();


      if (IsEmpty() && pot.Count > 0)
      {
        FindWinner();
      }

      RemoveLoser();
    }
  }

  
   /// <summary>
   /// Controls the pause menu when in auto mode.
   ///Selects different options not including exit and resume.
   /// </summary>
  void AutoModePauseMenu()
  {
    table.pauseflag = !table.pauseflag;
    table.PauseMenu();

    float option = UnityEngine.Random.Range(1, 4);

    switch(option)
    {
      case 1:
      {
          pause.GetComponentInChildren<PauseMenu>().Options();
          break;
      }
      case 2:
      {
          pause.GetComponentInChildren<PauseMenu>().Sound();
          break;
      }
      case 3:
      {
          pause.GetComponentInChildren<PauseMenu>().Graphics();
          break;
      }
    }
  }

  /// <summary>
  /// Adjusts the number of players from alpha keys 1 - 5
  /// </summary>
  void AdjustNumberOfPlayers()
  {
    if (!IsEmpty()) return;
    bool trigger = false;

    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
      ChangePlayers(2);
      trigger = true;
    }
    else if (Input.GetKeyDown(KeyCode.Alpha2))
    {
      ChangePlayers(3);
      trigger = true;
    }
    else if (Input.GetKeyDown(KeyCode.Alpha3))
    {
      ChangePlayers(4);
      trigger = true;
    }
    else if (Input.GetKeyDown(KeyCode.Alpha4))
    {
      ChangePlayers(5);
      trigger = true;
    }
    else if (Input.GetKeyDown(KeyCode.Alpha5))
    {
      ChangePlayers(6);
      trigger = true;
    }

    if (trigger)
    {
      table.players.Clear();

      table.CreatePlayers();
      table.CreateCoins();

      playactive = true;
      trigger = false;
      styleflag = true;
    }
  }

  /// <summary>
  /// Changes the number of players at the table.
  /// </summary>
  /// <param name="numberofplayers"></param>
  void ChangePlayers(int numberofplayers)
  {
    if (table.numberofplayers == numberofplayers) return;

    table.numberofplayers = numberofplayers;

    for (int i = 0; i < table.players.Count; ++i)
    {
      FoldAction(table.players[i], -i);

      int index = 0;
      foreach (GameObject coin in table.players[i].coins)
      {
        MoveCoinToPot(table.players[i], coin, index++);
      }

      table.players[i].coins.RemoveRange(0, table.players[i].coins.Count);

      GameObject t = table.players[i].GameObject();

      CreateFadeAction(t, 0.0f, Ease.EASE.EASE_OUT, 1.0f, 3.0f);

    }
  }

  /// <summary>
  /// Moves the coins in the pot to the winning player.
  /// </summary>
  /// <param name="player"></param>
  /// <param name="coin"></param>
  /// <param name="index"></param>
  public void MoveCoinToPot(Player player, GameObject coin, int index)
  {
    float delay = 1.0f;
    Vector3 playerpos = player.coins[index].transform.position;

    float x = 100.0f;
    float y = 100.0f;

    CreateMoveAction(player.coins[index], playerpos, new Vector3(x, y), delay + ((float)index / 10.0f), 5.0f);
  }

  /// <summary>
  /// Adjusts the number of players at the table.
  /// </summary>
  /// <param name="newPlayerCount"></param>
  public void AdjustNumberOfPlayers(int newPlayerCount)
  {
    // Adjust players to match the newPlayerCount
    int currentCount = table.players.Count;

    if (newPlayerCount > currentCount)
    {
      // Add players
      for (int i = currentCount; i < newPlayerCount; i++)
      {
        // Add a new player to the table
        Player newPlayer = new Player(0, "Blue"); // Assuming Player has a default constructor
                                         // Initialize newPlayer as needed
        table.players.Add(newPlayer);
      }
    }
    else if (newPlayerCount < currentCount)
    {
      // Remove players
      // Note: Consider how to handle players' bets, cards, etc., before removing them.
      for (int i = currentCount; i > newPlayerCount; i--)
      {
        // Remove the last player. Consider removing specific players based on your game logic.
        table.players.RemoveAt(table.players.Count - 1);
      }
    }
  }

  /// <summary>
  /// Sets the style of play for each AI.
  /// </summary>
  void SetPlayerPlayStyle()
  {
    //if (styleflag == false) return;
    for(int i = 0; i < table.players.Count; ++i)
    {
      int x = i % table.players.Count;
      SetStyle(table.players[i], x);
    }

    styleflag = false;
  }

  /// <summary>
  /// Sets the specific style for a given player.
  /// </summary>
  /// <param name="player"></param>
  /// <param name="playernumber"></param>
  void SetStyle(Player player, int playernumber)
  {
    switch(playernumber)
    {
      case 0: //Player 1
      {
          player.bettrigger = false;
          player.foldtrigger = false;
          player.betamount = 1;
          break;
      }
      case 1: //player 2
      {
          player.bettrigger = true;
          player.foldtrigger = false;
          player.betamount = 2;
          break;
      }
      case 2: //player 3
      {
          player.bettrigger = true;
          player.foldtrigger = false;
          player.betamount = 1;
          break;
      }
      case 3: //Player 4
      {
          player.bettrigger = false;
          player.foldtrigger = true;
          player.betamount = 1;
          break;
      }
      case 4: //player 5
      {
          player.bettrigger = true;
          player.foldtrigger = false;
          player.betamount = UnityEngine.Random.Range(1, 4);
          break;
      }
      case 5: //player 6
      {
          player.bettrigger = true;
          player.foldtrigger = false;
          player.betamount = playernumber - 1;
          break;
      }
      default:
        break;
    }
  }

  /// <summary>
  /// Gets the players bet amount
  /// </summary>
  /// <returns></returns>
  public int GetPlayerBetAmount()
  {
    return UnityEngine.Random.Range(1, table.coinsperplayer);
  }

  /// <summary>
  /// Determines if the game is over.
  /// </summary>
  /// <returns></returns>
  bool GameOver()
  {
    if (table.round > table.totalrounds || table.players.Count == 1)
    {
      int max = 0;
      int playernumber = 0;
      //table.round = table.round - 1;

      foreach (Player player in table.players)
      {
        if (max < player.coins.Count)
        {
          max = player.coins.Count;
          playernumber = player.playernumber;
        }
      }

      Vector3 position = table.ConvertToPolarCoordinates(90.0f, 2.0f);
      GameObject roundcount = Instantiate(table.Prefabs["Player"], position, Quaternion.identity);
      roundcount.GetComponent<TMP_Text>().text = "Player " + playernumber.ToString() + " WINS!";
      roundcount.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
      roundcount.GetComponent<TMP_Text>().color = UnityEngine.Color.black;
      roundcount.GetComponent<TMP_Text>().fontSize = 5.0f;

      data.WriteLineToPauseFile(table.resumecount.ToString() + "," 
                              + table.optionscount.ToString() + ","
                              + table.soundcount.ToString() + ","
                              + table.graphicscount.ToString());

      return true;
    }
    else
      return false;
  }

  /// <summary>
  /// The folding action for a given player.
  /// </summary>
  /// <param name="player"></param>
  /// <param name="index"></param>
  public void FoldAction(Player player, float index)
  {
    if (player.cards.Count == 0) return;

    if(player.cards.Count > 0)
    {
      float delay = 4.0f;

      for (int i = 0; i <  player.cards.Count; i++)
      {
        Vector3 startpos = player.cards[i].transform.position;
        Vector3 endpos = table.ConvertToPolarCoordinates(-20.0f, 10.0f);

        CreateFlipAction(player.cards[i], 0.0f, 1.0f);

        CreateMoveAction(player.cards[i], startpos, endpos, delay + ((float)index / (float)table.players.Count), 1.0f);

        table.maindeck.Add(player.cards[i]);
      }

      player.cards.RemoveRange(0, player.cards.Count);
    }

    table.Shuffle<GameObject>(table.maindeck);

    player.foldtrigger = false;
  }

  /// <summary>
  /// The bet action for a given player.
  /// </summary>
  /// <param name="player"></param>
  /// <param name="index"></param>
  public void BetAction(Player player, float index)
  {
    if (player == null) return;
    if (player.cards.Count == 0) return;
    if (player.coins.Count == 0) return;

    Vector3 playerpos = Vector3.zero;

    if (player.coins.Count < player.betamount)
      betamount = player.coins.Count;
    else
      betamount = player.betamount;

    float delay = 2.0f;
    //Move the coin to the pot. DO NOT CHANGE WHO THE COIN BELONGS TO.
    for (int i = 0; i < betamount; ++i)
    {
      playerpos = player.coins[i].transform.position;

      float x = UnityEngine.Random.Range(-0.5f, 0.5f);
      float y = UnityEngine.Random.Range(-0.5f, 0.5f);
      float z = UnityEngine.Random.Range(-0.5f, 0.5f);


      CreateMoveAction(player.coins[i], playerpos, new Vector3(x, y, z), delay + ((float)index / 5.0f), 1.0f);

      //Create move action
      pot.Add(player.coins[i]);
    }

    player.coins.RemoveRange(0, betamount);

    player.bettrigger = false;
  }

  /// <summary>
  /// Determines the winner of a hand.
  /// </summary>
  public void FindWinner()
  {
    int maxscore = 0;


    foreach(Player player in table.players)
    {
      if (player.coins.Count == 0) continue;

      player.playerscore = GetPlayerScore(player);

      if (player.playerscore > maxscore)
      {
        maxscore = player.playerscore;
        winningplayer = player;
      }
    }

    summary = table.round.ToString() + ",";
    for (int i = 0; i < table.players.Count; ++i)
    {
      summary += table.players[i].playerscore.ToString() + ",";
    }

    data.WriteLineToHandFile(summary);

    if (pot.Count > 0)
      MoveCoinsToWinner(winningplayer);
  }

  /// <summary>
  /// Determines the number of players left in the game.
  /// </summary>
  /// <returns></returns>
  int PlayersRemaining()
  {
    int totalplayerremaining = 0;

    foreach(Player player in table.players)
    {
      if(player.coins.Count != 0)
      {
        totalplayerremaining++;
      }
    }

    return totalplayerremaining;
  }

 /// <summary>
 /// Removes the loser from the table.
 /// </summary>
  public void RemoveLoser()
  {
    for(int i = 0; i < table.players.Count; i++)
    {
      if (table.players[i].coins.Count == 0)
      {
        for(int j = 0; j < table.players[i].cards.Count; ++j)
        {
          CreateMoveAction(table.players[i].cards[j], table.players[i].cards[j].transform.position, table.maindeck[0].transform.position);
          
          table.maindeck.Add(table.players[i].cards[j]);
        }

        table.players[i].cards.RemoveRange(0, table.players[i].cards.Count);

        CreateFadeAction(table.players[i].GameObject(), 0.0f, Ease.EASE.EASE_OUT, 1.0f, 2.0f);

        table.players.RemoveAt(i);
        --i;
      }
    }

    if (PlayersRemaining() == 1)
      GameOver();
  }

  /// <summary>
  /// Moves the pot to the winning player.
  /// </summary>
  /// <param name="player"></param>
  public void MoveCoinsToWinner(Player player)
  {
    if (player == null) return;
    foreach(GameObject coin in pot)
    {
      int index = player.coins.Count;
      player.coins.Insert(index,coin);

      CreateMoveAction(coin, coin.transform.position, player.coins[0].transform.position, 1.0f, 2.0f);
    }

    pot.Clear();

    NewRound();

    summary = (table.round - 1).ToString() + ",";

    for (int i = 0; i < table.players.Count; ++i)
    {
      if (table.players[i].foldtrigger == true)
        summary += table.players[i].coins.Count.ToString() + " (F) " + ",";
      else if (table.players[i] == player)
        summary += table.players[i].coins.Count.ToString() + " (W) " + ",";
      else
        summary += table.players[i].coins.Count.ToString() + " (L) " + ",";
    }

    data.WriteLineToPlayerFile(summary);
  }

  /// <summary>
  /// Resets the game for a new round.
  /// </summary>
  void NewRound()
  {
    for(int i = 0; i < table.players.Count; ++i)
    {
      FoldAction(table.players[i], i);
    }

    table.Shuffle<GameObject>(table.maindeck);

    if(gamedt != 0.0f)
      ++table.round;

    playactive = true;
  }

  /// <summary>
  /// Sets all of the player's fold status.
  /// </summary>
  /// <param name="status"></param>
  public void SetGroupFoldStatus(bool status)
  {
    foreach (Player player in table.players)
    {
      player.foldtrigger = status;
    }
  }

 /// <summary>
 /// Deals new cards at the begining of a round.
 /// </summary>
  public void DealCardsNewRound()
  {
    if (playactive == false) return;

    //Move 5 cards from the deck to each player.
    foreach(Player player in table.players)
    {
      if (player.cards.Count == table.cardsperplayer)
        continue;

      if (player.outOrIn == false)
        continue;

      for(int i = 0; i < table.cardsperplayer; ++i)
      {
        table.maindeck[i].GetComponent<Card>().player = player.playernumber;
        player.cards.Add(table.maindeck[i]);

        //Remove those five cards from the deck list.
      }
      table.maindeck.RemoveRange(0, table.cardsperplayer); //pass

      float index = -table.cardsperplayer / 2.0f * 0.35f;

      float posY = 0.0f;

      if (player.transform.position.y < 0)
        posY = player.transform.position.y + 0.8f;
      else
        posY = player.transform.position.y - 0.8f;

      float delay;
      if (table.round < 2)
        delay = 0.5f;
      else
        delay = 6.0f;

      foreach (GameObject card in  player.cards)
      {
        Vector3 endpos = new Vector3(player.transform.position.x + index, posY);
        Vector3 startpos = table.ConvertToPolarCoordinates(-20.0f, 10.0f);

        if(player.playernumber != 1)
          CreateFlipAction(card, 180.0f, 0.0f, 1.0f);
        else
          CreateFlipAction(card, 0.0f, 0.0f, 1.0f);

        //Create a translate action for each card
        CreateMoveAction(card, startpos, endpos,delay, 3.0f);

        delay += 0.25f;
        index += 0.45f;
      }
    }

    playactive = false;
  }

  /// <summary>
  /// Determines the score for each player.
  /// </summary>
  /// <param name="player"></param>
  /// <returns></returns>
  public int GetPlayerScore(Player player)
  {
    int sum = 0;

    foreach(GameObject card in player.cards)
    {
      sum += card.GetComponent<Card>().number;      
    }

    return sum;
  }

  /// <summary>
  /// Runs the pause action list and the main action list.
  /// </summary>
  /// <returns></returns>
  public bool RunActions()
  {
    for (int i = 0; i < pauseactions.Count; i++)
    {
      if (pauseactions[i].incrementtime(pausedt))
      {
        if (!pauseactions[i].Update(pausedt))
        {
          pauseactions.Remove(pauseactions[i]);
          --i;
        }
      }
    }

    //loop through the list action.
    for (int i = 0; i < actions.Count; i++)
    {
      if (actions[i].incrementtime(gamedt))
      {
        if (logflag == true && i % 10 == 0)
          log.HandleLog("Action Name: " + actions[i].ToString() + " Action Percent: " + actions[i].percent, "Action", LogType.Log);

        if (!actions[i].Update(gamedt))
        {
          actions.Remove(actions[i]);
          --i;
        }

      }
    }

    if (actions.Count == 0)
      return true;
    else
      return false;
  }

  /// <summary>
  /// Creates a move action via translation.
  /// </summary>
  /// <param name="Object"></param>
  /// <param name="startpos"></param>
  /// <param name="endpos"></param>
  /// <param name="delay"></param>
  /// <param name="duration"></param>
  public void CreateMoveAction(GameObject Object, Vector3 startpos, Vector3 endpos, float delay = 0.0f, float duration = 1.0f)
  {
    if (logflag == true)
      log.HandleLog("Translate action created", "Action", LogType.Log);

    Translate translate;

    translate = new Translate(startpos, endpos, Ease.EASE.EASE_IN_OUT);
    translate.ActionObject = Object;
    translate.duration = duration;
    translate.delay = delay;
   
    //add it to the list.
    actions.Add(translate);
  }

  /// <summary>
  /// Creates a move action via translation for the pause menu.
  /// </summary>
  /// <param name="Object"></param>
  /// <param name="startpos"></param>
  /// <param name="endpos"></param>
  /// <param name="delay"></param>
  /// <param name="duration"></param>
  public void CreatePauseMoveAction(GameObject Object, Vector3 startpos, Vector3 endpos, float delay = 0.0f, float duration = 1.0f)
  {
    Translate translate;

    translate = new Translate(startpos, endpos, Ease.EASE.EASE_IN_OUT);
    translate.ActionObject = Object;
    translate.duration = duration;
    translate.delay = delay;

    //add it to the list.
    pauseactions.Add(translate);
  }

  public void CreatePauseScaleAction(GameObject localobject, float oldscale, float scale = 1.0f, float delay = 0.0f, float duration = 1.0f)
  {
    Scale localscale;

    localscale = new Scale(scale, Ease.EASE.EASE_IN_OUT);
    localscale.ActionObject = localobject;
    localscale.duration = duration;
    localscale.delay = delay;
    localscale.oldscale = new Vector3(oldscale, oldscale);


    pauseactions.Add(localscale);
  }

  /// <summary>
  /// Creates a rotate action.
  /// </summary>
  /// <param name="Object"></param>
  /// <param name="rotationamount"></param>
  /// <param name="delay"></param>
  /// <param name="duration"></param>
  /// <param name="listnumber"></param>
  public void CreateRotateAction(GameObject Object, float rotationamount, float delay = 0.0f, float duration = 5.0f, int listnumber = 1)
  {
    if (logflag == true)
      log.HandleLog("Rotate action created", "Action", LogType.Log);

    Rotate rotate = new Rotate(rotationamount);

    rotate.duration = duration;
    rotate.ActionObject = Object;
    rotate.delay = delay;

    if (listnumber == 1)
      actions.Add(rotate);
    else if (listnumber == 2)
      pauseactions.Add(rotate);
  }

  /// <summary>
  /// Creates a flip action.
  /// </summary>
  /// <param name="Object"></param>
  /// <param name="rotation"></param>
  /// <param name="delay"></param>
  /// <param name="duration"></param>
  public void CreateFlipAction(GameObject Object, float rotation, float delay = 0.0f, float duration = 5.0f)
  {
    if (logflag == true)
      log.HandleLog("Flip action created", "Action", LogType.Log);

    Flip flip = new Flip(Object, rotation, delay, duration);

    actions.Add(flip);
  }

  /// <summary>
  /// Creates a fade action.
  /// </summary>
  /// <param name="objecttype"></param>
  /// <param name="alpha"></param>
  /// <param name="easetype"></param>
  /// <param name="delay"></param>
  /// <param name="duration"></param>
  public void CreateFadeAction(GameObject objecttype, float alpha, Ease.EASE easetype, float delay, float duration)
  {
    if (logflag == true)
      log.HandleLog("Fade action created", "Action", LogType.Log);

    Fade fade = new Fade(objecttype, alpha, easetype, duration, delay);

    actions.Add(fade);
  }

  public void CreatePauseFadeAction(GameObject objecttype, float newalpha, Ease.EASE easetype, float delay, float duration)
  {
    if (logflag == true)
      log.HandleLog("Fade action created", "Action", LogType.Log);

    FadeGO fade = new FadeGO(objecttype, newalpha, easetype);
    fade.delay = delay;
    fade.duration = duration;

    pauseactions.Add(fade);
  }

  /// <summary>
  /// Resets the flags for the button and main play.
  /// </summary>
  public void ResetButtonFlags()
  {
    bettriggered = false;
    playactive = false;
    foldtriggered = false;
  }

  /// <summary>
  /// Checks to see if the main action list is empty.
  /// </summary>
  /// <returns></returns>
  public bool IsEmpty()
  {
    if (actions != null)
      return actions.Count == 0;
    else
      return true;
  }
}
