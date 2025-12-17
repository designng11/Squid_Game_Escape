using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TypingTextEffect : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private Text textComponent;
    [SerializeField] private TextMeshProUGUI textMeshProComponent;
    
    [Header("텍스트 설정")]
    [SerializeField] private string[] texts;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float delayBetweenTexts = 1f;
    
    [Header("자동 재생 설정")]
    [SerializeField] private bool autoPlay = true;
    [SerializeField] private bool loop = false;
    
    [Header("배경 설정")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer;
    [SerializeField] private Sprite finalBackgroundSprite;
    [SerializeField] private Color blackBackgroundColor = Color.black;
    [SerializeField] private float backgroundTransitionDuration = 1f;
    
    [Header("버튼 설정")]
    [SerializeField] private Button finalButton;
    [SerializeField] private float textFadeOutDuration = 0.5f;
    
    private int currentTextIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Color initialBackgroundColor;
    private GameObject textGameObject;
    
    void Start()
    {
        if (textComponent == null && textMeshProComponent == null)
        {
            textMeshProComponent = GetComponent<TextMeshProUGUI>();
            if (textMeshProComponent == null)
            {
                textComponent = GetComponent<Text>();
            }
        }
        
        if (textComponent == null && textMeshProComponent == null)
        {
            return;
        }
        
        if (backgroundImage == null && backgroundSpriteRenderer == null)
        {
            backgroundImage = GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = GetComponentInParent<Image>();
                if (backgroundImage == null)
                {
                    backgroundSpriteRenderer = GetComponent<SpriteRenderer>();
                    if (backgroundSpriteRenderer == null)
                    {
                        backgroundSpriteRenderer = GetComponentInParent<SpriteRenderer>();
                    }
                }
            }
        }
        
        if (textMeshProComponent != null)
        {
            textGameObject = textMeshProComponent.gameObject;
        }
        else if (textComponent != null)
        {
            textGameObject = textComponent.gameObject;
        }
        
        InitializeBlackBackground();
        
        if (finalButton != null)
        {
            finalButton.gameObject.SetActive(false);
        }
        
        SetText("");
        
        if (autoPlay && texts != null && texts.Length > 0)
        {
            StartTyping();
        }
    }
    
    private void InitializeBlackBackground()
    {
        if (backgroundImage != null)
        {
            initialBackgroundColor = backgroundImage.color;
            backgroundImage.color = blackBackgroundColor;
            
            if (backgroundImage.sprite != null)
            {
                backgroundImage.sprite = null;
            }
        }
        else if (backgroundSpriteRenderer != null)
        {
            initialBackgroundColor = backgroundSpriteRenderer.color;
            backgroundSpriteRenderer.color = blackBackgroundColor;
            
            if (backgroundSpriteRenderer.sprite != null)
            {
                backgroundSpriteRenderer.sprite = null;
            }
        }
    }
    
    public void StartTyping()
    {
        if (texts == null || texts.Length == 0)
        {
            return;
        }
        
        currentTextIndex = 0;
        StartCoroutine(TypingSequence());
    }
    
    IEnumerator TypingSequence()
    {
        while (currentTextIndex < texts.Length)
        {
            yield return StartCoroutine(TypeText(texts[currentTextIndex]));
            
            if (currentTextIndex == texts.Length - 1)
            {
                yield return StartCoroutine(ChangeBackgroundToFinal());
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenTexts);
            }
            
            currentTextIndex++;
        }
        
        if (loop)
        {
            currentTextIndex = 0;
            InitializeBlackBackground();
            
            if (textGameObject != null)
            {
                textGameObject.SetActive(true);
                CanvasGroup canvasGroup = textGameObject.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }
            
            if (finalButton != null)
            {
                finalButton.gameObject.SetActive(false);
            }
            
            StartCoroutine(TypingSequence());
        }
    }
    
    IEnumerator ChangeBackgroundToFinal()
    {
        Coroutine textFadeOut = null;
        Coroutine backgroundChange = null;
        
        if (textGameObject != null)
        {
            textFadeOut = StartCoroutine(FadeOutText());
        }
        
        if (finalBackgroundSprite != null)
        {
            backgroundChange = StartCoroutine(ChangeBackgroundSprite());
        }
        
        if (textFadeOut != null)
        {
            yield return textFadeOut;
        }
        if (backgroundChange != null)
        {
            yield return backgroundChange;
        }
        
        if (textGameObject != null)
        {
            textGameObject.SetActive(false);
        }
        
        if (finalButton != null)
        {
            finalButton.gameObject.SetActive(true);
        }
    }
    
    IEnumerator FadeOutText()
    {
        if (textGameObject == null) yield break;
        
        CanvasGroup canvasGroup = textGameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = textGameObject.AddComponent<CanvasGroup>();
        }
        
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < textFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / textFadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    IEnumerator ChangeBackgroundSprite()
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = finalBackgroundSprite;
            
            Color targetColor = Color.white;
            Color startColor = backgroundImage.color;
            float elapsedTime = 0f;
            
            while (elapsedTime < backgroundTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / backgroundTransitionDuration;
                backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }
            
            backgroundImage.color = targetColor;
        }
        else if (backgroundSpriteRenderer != null)
        {
            backgroundSpriteRenderer.sprite = finalBackgroundSprite;
            
            Color targetColor = Color.white;
            Color startColor = backgroundSpriteRenderer.color;
            float elapsedTime = 0f;
            
            while (elapsedTime < backgroundTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / backgroundTransitionDuration;
                backgroundSpriteRenderer.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }
            
            backgroundSpriteRenderer.color = targetColor;
        }
    }
    
    IEnumerator TypeText(string text)
    {
        isTyping = true;
        SetText("");
        
        foreach (char letter in text.ToCharArray())
        {
            SetText(GetText() + letter);
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
    }
    
    private void SetText(string text)
    {
        if (textMeshProComponent != null)
        {
            textMeshProComponent.text = text;
        }
        else if (textComponent != null)
        {
            textComponent.text = text;
        }
    }
    
    private string GetText()
    {
        if (textMeshProComponent != null)
        {
            return textMeshProComponent.text;
        }
        else if (textComponent != null)
        {
            return textComponent.text;
        }
        return "";
    }
    
    public void SkipTyping()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
            
            if (currentTextIndex < texts.Length)
            {
                SetText(texts[currentTextIndex]);
            }
        }
    }
    
    public void NextText()
    {
        if (isTyping)
        {
            SkipTyping();
        }
        else
        {
            currentTextIndex++;
            if (currentTextIndex >= texts.Length)
            {
                if (loop)
                {
                    currentTextIndex = 0;
                }
                else
                {
                    return;
                }
            }
            StartCoroutine(TypeText(texts[currentTextIndex]));
        }
    }
    
    public void StopTyping()
    {
        StopAllCoroutines();
        isTyping = false;
        SetText("");
    }
    
    public void SetTexts(string[] newTexts)
    {
        texts = newTexts;
        currentTextIndex = 0;
    }
    
    public void SetTypingSpeed(float speed)
    {
        typingSpeed = Mathf.Max(0.01f, speed);
    }
}
