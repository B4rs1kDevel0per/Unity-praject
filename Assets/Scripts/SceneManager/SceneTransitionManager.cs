using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Glitch settings")]
    [SerializeField] private int columnCount = 18;
    [SerializeField] private float baseColumnDuration = 0.35f;
    [SerializeField] private float staggerPerColumn = 0.02f;
    [SerializeField] private float jitterAmount = 30f;
    [SerializeField] private int noiseBlockCount = 10;
    [SerializeField] private Color primaryColor = Color.black;
    [SerializeField] private Color glitchTint = new Color(0.15f, 1f, 0.35f, 1f);

    [Header("Static noise")]
    [SerializeField] private int noiseTexWidth = 64;
    [SerializeField] private int noiseTexHeight = 36;
    [SerializeField] private float noiseRefreshRate = 0.05f; // сек между обновлениями статики

    private RectTransform[] columns;
    private Image[] columnImages;
    private RectTransform[] noiseBlocks;
    private Image[] noiseImages;
    private RectTransform canvasRT;
    private CanvasGroup blockerGroup;

    private RawImage staticNoiseImage;
    private Texture2D noiseTexture;
    private Color32[] noisePixels;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildGlitchCanvas();
    }

    private void BuildGlitchCanvas()
    {
        GameObject canvasGO = new GameObject("GlitchTransitionCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasRT = canvasGO.GetComponent<RectTransform>();

        // ключевой фикс: раскастблок отключён по умолчанию, включается ТОЛЬКО во время перехода
        blockerGroup = canvasGO.AddComponent<CanvasGroup>();
        blockerGroup.blocksRaycasts = false;
        blockerGroup.interactable = false;

        BuildStaticNoise(canvasGO.transform);

        // вертикальные колонки
        columns = new RectTransform[columnCount];
        columnImages = new Image[columnCount];
        float colWidthPercent = 1f / columnCount;

        for (int i = 0; i < columnCount; i++)
        {
            GameObject go = new GameObject($"Column_{i}");
            go.transform.SetParent(canvasGO.transform, false);

            Image img = go.AddComponent<Image>();
            img.color = primaryColor;
            img.raycastTarget = false; // важно
            columnImages[i] = img;

            RectTransform rt = img.rectTransform;
            columns[i] = rt;

            rt.anchorMin = new Vector2(i * colWidthPercent, 0f);
            rt.anchorMax = new Vector2((i + 1) * colWidthPercent, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // рандомные блоки-помехи
        noiseBlocks = new RectTransform[noiseBlockCount];
        noiseImages = new Image[noiseBlockCount];

        for (int i = 0; i < noiseBlockCount; i++)
        {
            GameObject go = new GameObject($"Noise_{i}");
            go.transform.SetParent(canvasGO.transform, false);

            Image img = go.AddComponent<Image>();
            img.color = new Color(glitchTint.r, glitchTint.g, glitchTint.b, 0f);
            img.raycastTarget = false; // важно
            noiseImages[i] = img;

            RectTransform rt = img.rectTransform;
            noiseBlocks[i] = rt;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
        }

        SetColumnsOffscreen(fromTop: true);
        SetColumnsAlpha(0f);
    }

    private void BuildStaticNoise(Transform parent)
    {
        GameObject go = new GameObject("StaticNoise");
        go.transform.SetParent(parent, false);

        staticNoiseImage = go.AddComponent<RawImage>();
        staticNoiseImage.raycastTarget = false; // важно

        RectTransform rt = staticNoiseImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        noiseTexture = new Texture2D(noiseTexWidth, noiseTexHeight, TextureFormat.RGBA32, false);
        noiseTexture.filterMode = FilterMode.Point; // пиксельные блоки, не размытие
        noiseTexture.wrapMode = TextureWrapMode.Clamp;
        noisePixels = new Color32[noiseTexWidth * noiseTexHeight];

        staticNoiseImage.texture = noiseTexture;

        Color c = staticNoiseImage.color;
        c.a = 0f;
        staticNoiseImage.color = c;
    }

    private void RandomizeNoiseTexture(float intensity)
    {
        for (int i = 0; i < noisePixels.Length; i++)
        {
            if (Random.value < intensity)
            {
                bool useTint = Random.value < 0.3f;
                Color32 col = useTint
                    ? (Color32)glitchTint
                    : new Color32(255, 255, 255, 255);

                byte a = (byte)Random.Range(60, 220);
                noisePixels[i] = new Color32(col.r, col.g, col.b, a);
            }
            else
            {
                noisePixels[i] = new Color32(0, 0, 0, 0);
            }
        }

        noiseTexture.SetPixels32(noisePixels);
        noiseTexture.Apply(false);
    }

    private void SetColumnsAlpha(float a)
    {
        foreach (var img in columnImages)
        {
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }

    private void SetColumnsOffscreen(bool fromTop)
    {
        float height = canvasRT.rect.height > 0 ? canvasRT.rect.height : 1080f;
        float dir = fromTop ? 1f : -1f;

        foreach (var rt in columns)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, dir * height);
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;
        blockerGroup.blocksRaycasts = true; // блокируем клики ТОЛЬКО пока идёт переход

        yield return StartCoroutine(GlitchIn());

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
            yield return null;

        load.allowSceneActivation = true;

        while (!load.isDone)
            yield return null;

        yield return StartCoroutine(GlitchOut());

        blockerGroup.blocksRaycasts = false; // снова пропускаем клики
        isTransitioning = false;
    }

    private IEnumerator GlitchIn()
    {
        SetColumnsAlpha(1f);
        SetStaticAlpha(1f);
        float height = canvasRT.rect.height > 0 ? canvasRT.rect.height : 1080f;

        float[] delays = new float[columnCount];
        for (int i = 0; i < columnCount; i++)
            delays[i] = i * staggerPerColumn;

        bool[] fromTop = new bool[columnCount];
        for (int i = 0; i < columnCount; i++)
            fromTop[i] = Random.value > 0.5f;

        float totalDuration = baseColumnDuration + delays[columnCount - 1];
        float t = 0f;
        float noiseTimer = 0f;

        StartCoroutine(NoiseBlocksRoutine(totalDuration));

        while (t < totalDuration)
        {
            t += Time.deltaTime;
            noiseTimer += Time.deltaTime;

            if (noiseTimer >= noiseRefreshRate)
            {
                noiseTimer = 0f;
                RandomizeNoiseTexture(Random.Range(0.05f, 0.25f));
            }

            for (int i = 0; i < columnCount; i++)
            {
                float localT = Mathf.Clamp01((t - delays[i]) / baseColumnDuration);
                float eased = 1f - Mathf.Pow(1f - localT, 3f);

                float startY = fromTop[i] ? height : -height;
                float y = Mathf.Lerp(startY, 0f, eased);

                if (localT < 1f)
                    y += Random.Range(-jitterAmount, jitterAmount) * (1f - localT);

                columns[i].anchoredPosition = new Vector2(columns[i].anchoredPosition.x, y);
                columnImages[i].color = (localT < 0.6f && Random.value < 0.08f) ? glitchTint : primaryColor;
            }

            yield return null;
        }

        foreach (var rt in columns)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        foreach (var img in columnImages)
            img.color = primaryColor;

        SetStaticAlpha(0f);
    }

    private IEnumerator GlitchOut()
    {
        SetStaticAlpha(1f);
        float height = canvasRT.rect.height > 0 ? canvasRT.rect.height : 1080f;

        float[] delays = new float[columnCount];
        for (int i = 0; i < columnCount; i++)
            delays[i] = i * staggerPerColumn;

        bool[] toTop = new bool[columnCount];
        for (int i = 0; i < columnCount; i++)
            toTop[i] = Random.value > 0.5f;

        float totalDuration = baseColumnDuration + delays[columnCount - 1];
        float t = 0f;
        float noiseTimer = 0f;

        StartCoroutine(NoiseBlocksRoutine(totalDuration));

        while (t < totalDuration)
        {
            t += Time.deltaTime;
            noiseTimer += Time.deltaTime;

            if (noiseTimer >= noiseRefreshRate)
            {
                noiseTimer = 0f;
                RandomizeNoiseTexture(Random.Range(0.05f, 0.25f));
            }

            for (int i = 0; i < columnCount; i++)
            {
                float localT = Mathf.Clamp01((t - delays[i]) / baseColumnDuration);
                float eased = Mathf.Pow(localT, 3f);

                float endY = toTop[i] ? height : -height;
                float y = Mathf.Lerp(0f, endY, eased);

                if (localT < 1f && localT > 0f)
                    y += Random.Range(-jitterAmount, jitterAmount) * (1f - localT);

                columns[i].anchoredPosition = new Vector2(columns[i].anchoredPosition.x, y);
                columnImages[i].color = (localT < 0.4f && Random.value < 0.08f) ? glitchTint : primaryColor;
            }

            yield return null;
        }

        SetColumnsAlpha(0f);
        SetStaticAlpha(0f);
    }

    private void SetStaticAlpha(float a)
    {
        Color c = staticNoiseImage.color;
        c.a = a;
        staticNoiseImage.color = c;
    }

    private IEnumerator NoiseBlocksRoutine(float duration)
    {
        float width = canvasRT.rect.width > 0 ? canvasRT.rect.width : 1920f;
        float height = canvasRT.rect.height > 0 ? canvasRT.rect.height : 1080f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            foreach (var rt in noiseBlocks)
            {
                if (Random.value < 0.15f)
                {
                    rt.anchoredPosition = new Vector2(
                        Random.Range(-width / 2f, width / 2f),
                        Random.Range(-height / 2f, height / 2f));
                    rt.sizeDelta = new Vector2(Random.Range(4f, 24f), Random.Range(40f, height * 0.4f));

                    Image img = rt.GetComponent<Image>();
                    Color c = glitchTint;
                    c.a = Random.Range(0.3f, 0.8f);
                    img.color = c;
                }
                else
                {
                    Image img = rt.GetComponent<Image>();
                    Color c = img.color;
                    c.a = Mathf.Max(0f, c.a - Time.deltaTime * 3f);
                    img.color = c;
                }
            }

            yield return null;
        }

        foreach (var rt in noiseBlocks)
        {
            Image img = rt.GetComponent<Image>();
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }
}