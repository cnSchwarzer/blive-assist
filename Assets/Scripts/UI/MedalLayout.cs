using System.Collections;
using System.Collections.Generic;
using JoshH.UI; 
using UnityEngine;
using UnityEngine.UI; 

public class MedalLayout : MonoBehaviour
{
    public RectTransform rectTransform { get; private set; }
    
    public Image medalImage;
    public Color[] medalColors;
    public Gradient[] medalGradientColors;
    public Text medalName, medalLevel;
    public UIGradient gradient;
    public Image medalGuard; 
    public Sprite[] guardIcons;
    
    void OnEnable() {
        rectTransform = GetComponent<RectTransform>();
    } 
    
    public void SetMedal(string n, int level, int guardLevel) {
        if (n != null) { 
            var nameWidth = medalName.CalculateWidth(n);
            medalName.text = n; 
            medalName.rectTransform.anchoredPosition = new Vector2(guardLevel > 0 ? 25 : 5, 1.5f);
            medalName.rectTransform.sizeDelta = new Vector2(nameWidth, 0);
            medalLevel.text = level.ToString();
            rectTransform.sizeDelta = new Vector2(nameWidth + (guardLevel > 0 ? 56.5f : 36.5f), rectTransform.sizeDelta.y); 
            medalGuard.sprite = guardIcons[guardLevel];
             
            if (level == 0) {
                gradient.enabled = false; 
                medalImage.color = medalColors[0]; 
            } 
            else if (level >= 1 && level <= 20) {
                gradient.enabled = false;
                medalImage.color = medalColors[1 + (level - 1) / 4];
            }
            else if (level >= 21 && level <= 40) {
                gradient.enabled = true;
                gradient.LinearGradient = medalGradientColors[(level - 21) / 4];
            }
        
            if (guardLevel == 0) {
                medalGuard.gameObject.SetActive(false);
            }
            else if (guardLevel > 0) {
                medalGuard.gameObject.SetActive(true);
            }
        } 
    }
}
