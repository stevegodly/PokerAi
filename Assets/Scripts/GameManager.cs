using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] int startingMoney=100;
    [SerializeField] int startingBet=10;
    [SerializeField] TMP_Text moneyPoolText;
    int moneyPool=0;
    int curr_raise=10;
    string action;

    private HashSet<int> previouslyDiscardedIndices = new HashSet<int>();


    List<int> discardedIndices=new List<int>();
    List<int> tiebreakers=new List<int>();
    int rank;
    

    public enum GameStates
    {
        RoundStart,
        Ante,
        DealCards,
        Betting_Round_1,
        NextRound,
        Discard,
        Check_Winner,
        GameOver

    }
    public GameStates gameState;

    [SerializeField] TMP_Text messageText;
    [SerializeField] GameObject gameOver;

    [Header("Buttons")]
    [SerializeField] Button dealButton;
    [SerializeField] Button callButton;
    [SerializeField] Button foldButton;
    [SerializeField] Button raiseButton;
    [SerializeField] Button discardButton; 
    [SerializeField] Button fiveDolarButton;
    [SerializeField] Button tenDolarButton;
    [SerializeField] Button twentyfiveDolarButton;
    [SerializeField] Button fiftyDolarButton;
     
    [Header("Deck")]
    public List<string> deck;

    [Header("AI")]
    [SerializeField] Transform[] aiHandTransform;
    int aiMoney;
    int aiBetMoney=10;

    int round=0;
    List<string> aiHand;
    List<GameObject> aiCards=new List<GameObject>();
    [SerializeField] TMP_Text aiMoneyText;

    
    [Header("Player")]
    [SerializeField] Transform[] playerHandTransform;
    int playerMoney;
    int playerBetMoney;
    List<string> playerHand;
    List<GameObject> playerCards=new List<GameObject>();
    [SerializeField] TMP_Text playerMoneyText;
    [SerializeField] TMP_Text playerBetText;
    

    [SerializeField] GameObject CardPrefab;
    string[] ranks = {"2","3","4","5","6","7","8","9","10","J","Q","K","A"};
    string[] suits = {"s","h","d","c"};
    
    void Start()
    {
        PokerAI.LoadStrategy();
        InitMoney();
        StartCoroutine(TimebtwStates(GameStates.RoundStart,4f));
        
    }

    void Restart()
    {
        InitMoney();
        StartCoroutine(TimebtwStates(GameStates.RoundStart,1f));
    }

    void InitializeDeck()
    {
        deck=new List<string>();
        aiHand=new List<string>();
        playerHand=new List<string>(); 
        foreach(var suit in suits)
        {
            foreach(var rank in ranks)
            {
                deck.Add($"{rank}{suit}");
            }
        }
        ShuffleDeck();
    }

    void ShuffleDeck()
    {
        for(int i=deck.Count-1;i>0;i--)
        {
            string temp=deck[i];
            int randomIndex=UnityEngine.Random.Range(0,i+1);
            deck[i]=deck[randomIndex];
            deck[randomIndex]=temp;
        }
    }

    void DealCards()
    {
        for(int i=0;i<5;i++)
        {
            aiHand.Add(deck[0]);
            deck.RemoveAt(0);
            playerHand.Add(deck[0]);
            deck.RemoveAt(0);
        }
    }
    void AnimateCards()
    {
        foreach (var card in playerCards){
            Destroy(card);
        }
        playerCards.Clear();
        foreach (var card in aiCards){
            Destroy(card);
        }
        aiCards.Clear();

        for (int i=0;i<aiHand.Count;i++)
        {
            GameObject card=Instantiate(CardPrefab,aiHandTransform[i],true);
            string[] cardInfo=aiHand[i].Split(' ');
            card.GetComponent<Card>().SetCard(cardInfo[1],cardInfo[0],false);
            aiCards.Add(card);
        }

        for (int i=0;i<playerHand.Count;i++)
        {
            GameObject card=Instantiate(CardPrefab,playerHandTransform[i],false);
            string[] cardInfo=playerHand[i].Split(' ');
            card.GetComponent<Card>().SetCard(cardInfo[1],cardInfo[0],false);
            playerCards.Add(card);
        }
    }

    void GetHandRank(List<string> hand, out int rank, out List<int> tiebreakers)
    {
        
        var cardRanks = hand.Select(card => "23456789TJQKA".IndexOf(card[0])).OrderByDescending(r => r).ToList();
        var cardSuits = hand.Select(card => card[1]).ToList();

        
        var rankCounts = cardRanks.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        var suitCounts = cardSuits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        
        bool isFlush = suitCounts.Any(s => s.Value == 5);

        
        bool isStraight = cardRanks.Distinct().Count() == 5 && cardRanks.Max() - cardRanks.Min() == 4;

        
        if (cardRanks.SequenceEqual(new List<int> { 12, 3, 2, 1, 0 }))
        {
            isStraight = true;
            cardRanks = new List<int> { 3, 2, 1, 0, -1 }; 
        }

        
        if (isFlush && isStraight)
        {
            rank = 8; 
            tiebreakers = cardRanks;
        }
        else if (rankCounts.ContainsValue(4))
        {
            rank = 7; 
            tiebreakers = rankCounts.Where(kv => kv.Value == 4).Select(kv => kv.Key).ToList();
            tiebreakers.AddRange(rankCounts.Where(kv => kv.Value != 4).Select(kv => kv.Key));
        }
        else if (rankCounts.ContainsValue(3) && rankCounts.ContainsValue(2))
        {
            rank = 6; 
            tiebreakers = rankCounts.Where(kv => kv.Value == 3).Select(kv => kv.Key).ToList();
            tiebreakers.AddRange(rankCounts.Where(kv => kv.Value == 2).Select(kv => kv.Key));
        }
        else if (isFlush)
        {
            rank = 5; 
            tiebreakers = cardRanks;
        }
        else if (isStraight)
        {
            rank = 4; 
            tiebreakers = cardRanks;
        }
        else if (rankCounts.ContainsValue(3))
        {
            rank = 3; 
            tiebreakers = rankCounts.Where(kv => kv.Value == 3).Select(kv => kv.Key).ToList();
            tiebreakers.AddRange(rankCounts.Where(kv => kv.Value != 3).Select(kv => kv.Key));
        }
        else if (rankCounts.Count(kv => kv.Value == 2) == 2)
        {
            rank = 2; 
            tiebreakers = rankCounts.Where(kv => kv.Value == 2).Select(kv => kv.Key).OrderByDescending(k => k).ToList();
            tiebreakers.AddRange(rankCounts.Where(kv => kv.Value == 1).Select(kv => kv.Key));
        }
        else if (rankCounts.ContainsValue(2))
        {
            rank = 1; 
            tiebreakers = rankCounts.Where(kv => kv.Value == 2).Select(kv => kv.Key).ToList();
            tiebreakers.AddRange(rankCounts.Where(kv => kv.Value == 1).Select(kv => kv.Key));
        }
        else
        {
            rank = 0; 
            tiebreakers = cardRanks;
        }
    }

    int CompareTiebreakers(List<int> playerTiebreakers, List<int> aiTiebreakers)
    {
        for (int i = 0; i < playerTiebreakers.Count; i++)
        {
            if (playerTiebreakers[i] > aiTiebreakers[i])
            {
                return 1;
            }
            else if (playerTiebreakers[i] < aiTiebreakers[i])
            {
                return -1;
            }
        }

        return 0;
    }

    void InitMoney()
    {
        playerMoney=startingMoney;
        aiMoney=startingMoney;
        playerBetMoney=0;
        aiBetMoney=0;
        moneyPoolText.text="$"+moneyPool.ToString();
        playerMoneyText.text="$"+playerMoney.ToString();
        aiMoneyText.text="$"+aiMoney.ToString();
        playerBetText.text="$"+playerBetMoney.ToString();
    }
    void UpdatePlayerMoney(int amount)
    {
        playerMoney+=amount;
        playerMoneyText.text="$"+playerMoney.ToString();
    }
    void UpdatePlayerBet(int amount)
    {
        playerBetMoney+=amount;
        playerBetText.text="$"+playerBetMoney.ToString();
    }
    void UpdateAiBet(int amount)
    {
        aiBetMoney=amount;
    }
    void UpdateAiMoney(int amount)
    {
        aiMoney+=amount;
        aiMoneyText.text="$"+aiMoney.ToString();
    }
    void UpdateMoneyPool()
    {
        moneyPoolText.text="$"+moneyPool.ToString();
    }
    void ChangeGameState(GameStates newState)
    {
        gameState=newState;
        if(gameState==GameStates.RoundStart)
        {
            round=0;
            
            SetInfoMessage("Round Start");
            foreach (var card in playerCards){
                Destroy(card);
            }
            playerCards.Clear();
            foreach (var card in aiCards){
                Destroy(card);
            }
            aiCards.Clear();
            previouslyDiscardedIndices.Clear();
            StartCoroutine(TimebtwStates(GameStates.DealCards,0.25f));
        }
        else if(gameState==GameStates.Ante)
        {
            UpdatePlayerMoney(-startingBet);
            UpdateAiMoney(-startingBet);
            UpdatePlayerBet(startingBet);
            UpdateAiBet(startingBet);
            moneyPool+=startingBet*2;
            UpdateMoneyPool();
            UpdatePlayerBet(-startingBet);
            UpdateAiBet(0);
            SetInfoMessage("Ante placed!");
            curr_raise=0;
            action="";
            StartCoroutine(TimebtwStates(GameStates.Betting_Round_1,2f));
        }
        else if(gameState==GameStates.DealCards)
        {
            InitializeDeck();
            ShuffleDeck();
            DealCards();
            StartCoroutine(DealTheCards());
        }
        else if(gameState==GameStates.Betting_Round_1)
        {
            Debug.Log("Betting Round 1 State");
            dealButton.interactable=true;
            if(round==0 && (aiMoney<=0 || playerMoney<=0)) {
                if(aiMoney<=0) SetInfoMessage("AI has no money to bet!, Hence Round ends"); 
                else if(playerMoney<=0) SetInfoMessage("You have no money to bet!, Hence Round ends");
                else SetInfoMessage("No money to bet!");
                DisableButtons();
                StartCoroutine(TimebtwStates(GameStates.Check_Winner,3f));
                return;
            }
            if(round==0) callButton.GetComponentInChildren<TMP_Text>().text="CHECK";
            dealButton.GetComponentInChildren<TMP_Text>().text="DEAL";
            
            if(round==1) {
                DisableButtons();
                AiDecision(aiHand);
                Debug.Log("AI Decision made");
                
                
                if(action!="fold") StartCoroutine(TimebtwStates(GameStates.Discard,2.5f));
            }
            if(round==2 && (aiMoney<=0 || playerMoney<=0)) {
                if(aiMoney<=0) SetInfoMessage("AI has no money to bet!, Hence Round ends");
                else if(playerMoney<=0) SetInfoMessage("You have no money to bet!, Hence Round ends");
                else SetInfoMessage("No money to bet!");
                DisableButtons();
                StartCoroutine(TimebtwStates(GameStates.Check_Winner,3f));
                return;
            }
            if(round==2) {
                DisableButtons();
                AiDecision(aiHand);
                callButton.GetComponentInChildren<TMP_Text>().text="CALL";
            }    
            if(playerMoney>curr_raise && action!="fold") raiseButton.interactable=true;
            if(action!="fold") callButton.interactable=true;
            if(action!="fold") foldButton.interactable=true;
        }
        else if(gameState==GameStates.Discard)
        {       
            DisableButtons();
            AiDiscardAndDraw();
            if(discardedIndices.Count>0) SetInfoMessage("AI has discarded "+discardedIndices.Count+" cards!");
            
            callButton.GetComponentInChildren<TMP_Text>().text="DISCARD";
            callButton.interactable=true;
            dealButton.GetComponentInChildren<TMP_Text>().text="Next Round";
            dealButton.interactable=true;
        }
        else if(gameState==GameStates.NextRound)
        {
            Debug.Log("Next Round State");
            dealButton.GetComponentInChildren<TMP_Text>().text="Next Round";
            dealButton.interactable=true;
            moneyPool = 0;
            UpdateMoneyPool();
            UpdateAiBet(0);
            UpdatePlayerBet(-playerBetMoney);
            curr_raise=0;

            if(CheckGameOver()==1){
                DisableButtons();
                StartCoroutine(TimebtwStates(GameStates.GameOver,3f));
            }; 
        }

        else if(gameState == GameStates.Check_Winner)
        {
            DisableButtons();
            int playerRank, aiRank;
            List<int> playerTiebreakers, aiTiebreakers;

        
            PokerAI.GetHandRank(playerHand, out playerRank, out playerTiebreakers,out discardedIndices);
            Debug.Log("Player Hand Rank: " + playerRank);
            Debug.Log("Player Tiebreakers: " + string.Join(", ", playerTiebreakers));
            PokerAI.GetHandRank(aiHand, out aiRank, out aiTiebreakers,out discardedIndices);
            Debug.Log("AI Hand Rank: " + aiRank);
            Debug.Log("AI Tiebreakers: " + string.Join(", ", aiTiebreakers));

            if (playerRank > aiRank || (playerRank == aiRank && CompareTiebreakers(playerTiebreakers, aiTiebreakers) > 0))
            {
                
                UpdatePlayerMoney(moneyPool);
                SetInfoMessage("Player wins the round!");

            }
            else if (playerRank < aiRank || (playerRank == aiRank && CompareTiebreakers(playerTiebreakers, aiTiebreakers) < 0))
            {
                
                UpdateAiMoney(moneyPool);
                SetInfoMessage("AI wins the round!");
            }
            else
            {
                SetInfoMessage("It's a tie!");
                
            }
            for(int i=0;i<aiCards.Count;i++)
            {
                aiCards[i].GetComponent<Card>().FlipCard();
            }
            ChangeGameState(GameStates.NextRound);

        }
        else if(gameState == GameStates.GameOver){
            gameOver.SetActive(true);
        }
    }

    IEnumerator DealTheCards(){
        SetInfoMessage("Dealing Cards...");
        for (int i=0;i<aiHand.Count;i++)
        {
            GameObject aiCard=Instantiate(CardPrefab,aiHandTransform[i],false);
            string aiCardInfo = aiHand[i]; 
            string rank = aiCardInfo.Substring(0, aiCardInfo.Length-1); 
            string suit = aiCardInfo.Substring(aiCardInfo.Length-1);   
            aiCard.GetComponent<Card>().SetCard(suit,rank,true);
            aiCards.Add(aiCard);

            yield return new WaitForSeconds(0.25f);
        
            GameObject card=Instantiate(CardPrefab,playerHandTransform[i],false);
            string cardInfo = playerHand[i];
            string cardrank = cardInfo.Substring(0, cardInfo.Length-1); 
            string cardsuit = cardInfo.Substring(cardInfo.Length-1); 
            card.GetComponent<Card>().SetCard(cardsuit,cardrank,false);
            playerCards.Add(card);

            

            yield return new WaitForSeconds(0.25f);
        }
        StartCoroutine(TimebtwStates(GameStates.Ante));
    }

    IEnumerator TimebtwStates(GameStates gameState,float time=1.5f)
    {
        yield return new WaitForSeconds(time);
        ChangeGameState(gameState);
    }

    IEnumerator WaitForTouchThenContinue()
    {
        SetInfoMessage("Ai has folded, Tap to continue...");
        while (!Input.GetMouseButtonDown(0) && Input.touchCount == 0)
        {
            yield return null; 
        }

        ChangeGameState(GameStates.NextRound);
    }   

    void DisableButtons()
    {
        raiseButton.interactable=false;
        callButton.interactable=false;
        foldButton.interactable=false;
        dealButton.interactable=false;

        fiveDolarButton.interactable=false;
        tenDolarButton.interactable=false;
        twentyfiveDolarButton.interactable=false;
        fiftyDolarButton.interactable=false;
    }

    public void RaiseButton(){
        if(playerMoney<5 || playerMoney<=curr_raise) return;
        
        dealButton.interactable=false;
        if(playerMoney>=5) fiveDolarButton.interactable=true;
        if(playerMoney>=10) tenDolarButton.interactable=true;
        if(playerMoney>=25) twentyfiveDolarButton.interactable=true;
        if(playerMoney>=50) fiftyDolarButton.interactable=true;
        if(playerBetMoney>curr_raise) dealButton.interactable=true;
    }

    public void DealButton(){
        if(gameState==GameStates.Betting_Round_1 && round==0)
        {
            round++;
            DisableButtons();
            curr_raise=playerBetMoney;
            moneyPool+=playerBetMoney;
            UpdateMoneyPool();
            Debug.Log("Before round :"+round);
            if(round==1) {
                StartCoroutine(TimebtwStates(GameStates.Betting_Round_1,2f));
            }
        }
        else if(gameState==GameStates.Discard)
        {
            SetInfoMessage("2nd Betting Round");
            DisableButtons();
            round++;
            StartCoroutine(TimebtwStates(GameStates.Betting_Round_1,3.25f));
        }
        else if(round==2) {
            round++;
            DisableButtons();
            curr_raise=playerBetMoney;
            moneyPool+=playerBetMoney;
            UpdateMoneyPool();
            ChangeGameState(GameStates.Check_Winner);
        
        }
        else if(gameState==GameStates.NextRound)
        {
            Debug.Log("Next Round");
            ChangeGameState(GameStates.RoundStart);
        }

    }

    public void FoldButton()
    {
        DisableButtons();
        UpdateAiMoney(moneyPool);
        moneyPool=0;
        UpdateMoneyPool();
        SetInfoMessage("You folded!"); 
        StartCoroutine(TimebtwStates(GameStates.NextRound,0.5f));
        
    }

    public void SetBetAmount(int amount){
        UpdatePlayerMoney(-amount);
        UpdatePlayerBet(amount);
        DisableButtons();
        RaiseButton();
        if(playerBetMoney>curr_raise) dealButton.interactable=true;
    }

    public void CallButton()
    {
        if(round==0) {dealButton.interactable=true;return;}
        if(gameState==GameStates.Discard) {
            PlayerDiscardAndDraw();
            return;
        }
        int minCallAmount=Mathf.Min(aiBetMoney,playerMoney);
        UpdatePlayerBet(minCallAmount);
        int difference=aiBetMoney-playerBetMoney;
        if(difference>0) {
            moneyPool-=difference;
            UpdateAiMoney(difference);
            UpdateAiBet(minCallAmount);
            SetInfoMessage("Player has called all in!");
        }
        UpdatePlayerMoney(-minCallAmount);
        if(difference<=0) SetInfoMessage("You have called "+minCallAmount+"!");
        dealButton.interactable=true;
    } 

    public void AiDecision(List<string> hand)
    {

        Debug.Log("entered AI Decision");
        float prob=0;
        action = PokerAI.DecideAction(hand,out prob);
        if(round==2) curr_raise=0;
        if(action=="bet" && aiMoney<=curr_raise) action="call";
        if(action=="bet" && aiMoney<5) action="call";
        Debug.Log("AI action: "+action);
        if(action=="fold")
        {
            SetInfoMessage("AI has decided to fold!");
            UpdatePlayerMoney(moneyPool);
            moneyPool=0;
            UpdateMoneyPool();
            for(int i=0;i<aiCards.Count;i++)
            {
                aiCards[i].GetComponent<Card>().FlipCard();
            }
            StartCoroutine(WaitForTouchThenContinue());
        }
        else if(action=="call")
        {
            if(round==2){SetInfoMessage("AI has decided to check!");
                curr_raise=0;
                return;
            }
            
            int minCallAmount=Mathf.Min(aiMoney,playerBetMoney);
            moneyPool+=minCallAmount;
            UpdateAiBet(minCallAmount);
            int difference=playerBetMoney-minCallAmount;
            if(difference>0) {
                moneyPool-=difference;
                UpdatePlayerMoney(difference);
                UpdatePlayerBet(-difference);
                SetInfoMessage("AI has called all in!");
            }
            UpdateAiMoney(-minCallAmount);
            UpdateMoneyPool();
            if(difference<=0) SetInfoMessage("AI has decided to call!");
            
        }
        else if(action=="bet")
        {
            int rawBet = ((int)(prob * aiMoney))/2;
            int betAmount = (int)(Math.Round(rawBet / 5.0) * 5);
            if((playerBetMoney+5)< aiMoney) betAmount=Mathf.Max(playerBetMoney+5,betAmount);
            
            SetInfoMessage("AI has decided to raise "+betAmount+"!!");
            UpdateAiMoney(-betAmount);
            UpdateAiBet(betAmount);
            moneyPool+=betAmount;
            UpdateMoneyPool();
            curr_raise=betAmount;
        }
        UpdatePlayerBet(-playerBetMoney);
    }

    void PlayerDiscardAndDraw()
    {
        List<int> discardedIndices=GetPlayerDiscardedIndices();
        discardedIndices.Sort((a,b)=>b.CompareTo(a));
        
        foreach(var index in discardedIndices)
        {
            playerHand.RemoveAt(index);
            Destroy(playerCards[index]);
            playerCards.RemoveAt(index);
        }
        List<string> newCards=DrawNewCards(discardedIndices.Count);
        discardedIndices.Sort();
        for(int i=0;i<discardedIndices.Count;i++)
        {
            playerHand.Insert(discardedIndices[i],newCards[i]);
            GameObject card=Instantiate(CardPrefab,playerHandTransform[discardedIndices[i]],false);
            string cardInfo=newCards[i];
            string rank=cardInfo.Substring(0,cardInfo.Length-1);
            string suit=cardInfo.Substring(cardInfo.Length-1);
            Debug.Log("Card info: "+cardInfo+" rank: "+rank+" suit: "+suit);
            card.GetComponent<Card>().SetCard(suit,rank,false);
            playerCards.Insert(discardedIndices[i],card);
        }
    }

    void AiDiscardAndDraw()
    {
        
        PokerAI.GetHandRank(aiHand,out rank, out tiebreakers,out discardedIndices);
        discardedIndices.Sort((a,b)=>b.CompareTo(a));
        Debug.Log("AI discarded cards: "+discardedIndices.Count);
        
        
        foreach(var index in discardedIndices)
        {
            aiHand.RemoveAt(index);
            Destroy(aiCards[index]);
            aiCards.RemoveAt(index);
        }
        List<string> newCards=DrawNewCards(discardedIndices.Count);
        discardedIndices.Sort();
        for(int i=0;i<discardedIndices.Count;i++)
        {
            aiHand.Insert(discardedIndices[i],newCards[i]);
            GameObject card=Instantiate(CardPrefab,aiHandTransform[discardedIndices[i]],false);
            string cardInfo=newCards[i];
            string rank=cardInfo.Substring(0,cardInfo.Length-1);
            string suit=cardInfo.Substring(cardInfo.Length-1);
            card.GetComponent<Card>().SetCard(suit,rank,true);
            aiCards.Insert(discardedIndices[i],card);
        }
    }

    List<string> DrawNewCards(int amount)
    {
        List<string> newCards=new List<string>();
        for(int i=0;i<amount;i++)
        {
            newCards.Add(deck[0]);
            deck.RemoveAt(0);
        }
        return newCards;
    }

    List<int> GetPlayerDiscardedIndices()
    {
        List<int> discardedIndices=new List<int>();
        for(int i=0;i<playerCards.Count;i++)
        {
            if(!playerCards[i].GetComponent<Card>().IsShowing && !previouslyDiscardedIndices.Contains(i))
            {
                previouslyDiscardedIndices.Add(i);
                discardedIndices.Add(i);
            }
        }
        Debug.Log("Discarded count"+discardedIndices.Count);
        return discardedIndices;
    }

    
    int CheckGameOver()
    {
        if(playerMoney<startingBet)
        {   
            SetInfoMessage("Your money is gone...AI have won!");
            return 1;
            
        }
        else if(aiMoney<startingBet)
        {
            SetInfoMessage("You win...AI has lost all its money!");
            return 1;
        }
        return 0;
    }
    void SetInfoMessage(string message)
    {
        messageText.text=message;
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("Menu");
    }
    public void RestartGame()
    {
        gameOver.SetActive(false);
        Restart();
    }    
}
