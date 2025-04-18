using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [Header("Card Sprites")]
    [SerializeField] Sprite spadesSprite;
    [SerializeField] Sprite heartsSprite;
    [SerializeField] Sprite diamondsSprite;
    [SerializeField] Sprite clubsSprite;

    [Header("Card Images")]
    [SerializeField] Image middleImage;
    [SerializeField] Image topImage;    
    [SerializeField] Image bottomImage;
    
    [Header("Card Texts")]
    [SerializeField] TMP_Text topText;
    [SerializeField] TMP_Text bottomText;
    [SerializeField] TMP_Text midText;

    [Header("Card Back and Front")]
    [SerializeField] GameObject cardBack;
    [SerializeField] GameObject cardFront;

    bool isShowing;

    public void ShowCard(bool show)
    {
        isShowing = show;
        cardBack.SetActive(!show);
        cardFront.SetActive(show);
    }


    public void SetCard(string suit, string cardRank,bool isAi)
    {
        switch (suit)
        {
            case "s":
                middleImage.sprite = spadesSprite;
                topImage.sprite = spadesSprite;
                bottomImage.sprite = spadesSprite;
                break;
            case "h":
                middleImage.sprite = heartsSprite;
                topImage.sprite = heartsSprite;
                bottomImage.sprite = heartsSprite;
                break;
            case "d":
                middleImage.sprite = diamondsSprite;
                topImage.sprite = diamondsSprite;
                bottomImage.sprite = diamondsSprite;
                break;
            case "c":
                middleImage.sprite = clubsSprite;
                topImage.sprite = clubsSprite;
                bottomImage.sprite = clubsSprite;
                break;
        }

        GetComponent<Button>().interactable = !isAi;

        if(isAi){
            ShowCard(false);
        }
        else {
            ShowCard(true);
        }

        topText.text = cardRank;
        bottomText.text = cardRank;
        midText.text = cardRank;
    }

    public void FlipCard()
    {
        isShowing = !isShowing;
        ShowCard(isShowing);
    }
    public bool IsShowing => isShowing; 
}
