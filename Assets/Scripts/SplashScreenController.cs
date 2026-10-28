using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SplashScreenController : MonoBehaviour
{
    private const float MinimumDisplayTime = 2f;
    private const float FadeOutDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private RawImage splashImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateSplashScreen()
    {
        var sprite = Resources.Load<Sprite>("CodeCraftedSplash");
        if (sprite == null)
        {
            Debug.LogError("Splash sprite not found at Resources/CodeCraftedSplash.png.");
            return;
        }

        var root = new GameObject("CodeCrafted Splash Screen");
        DontDestroyOnLoad(root);

        var controller = root.AddComponent<SplashScreenController>();
        controller.CreateOverlay(sprite.texture);
    }

    private void CreateOverlay(Texture2D texture)
    {
        var canvasObject = new GameObject("Splash Canvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        var imageObject = new GameObject("Splash Image");
        imageObject.transform.SetParent(canvasObject.transform, false);
        splashImage = imageObject.AddComponent<RawImage>();
        splashImage.texture = texture;
        splashImage.color = Color.white;

        var imageRect = splashImage.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
    }

    private IEnumerator Start()
    {
        while (SceneManager.sceneCount == 0)
            yield return null;

        yield return null;
        CropImageToFillScreen();

        var games = FindObjectsByType<TwentyFortyEightGame>(FindObjectsSortMode.None);
        for (var i = 0; i < games.Length; i++)
            games[i].enabled = false;

        SetBannersVisible(false);
        yield return new WaitForSecondsRealtime(MinimumDisplayTime);

        var elapsed = 0f;
        while (elapsed < FadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        for (var i = 0; i < games.Length; i++)
        {
            if (games[i] != null)
                games[i].enabled = true;
        }

        SetBannersVisible(true);
        Destroy(gameObject);
    }

    private void CropImageToFillScreen()
    {
        var imageAspect = (float)splashImage.texture.width / splashImage.texture.height;
        var screenAspect = (float)Screen.width / Screen.height;
        var uvRect = new Rect(0f, 0f, 1f, 1f);

        if (screenAspect > imageAspect)
        {
            uvRect.height = imageAspect / screenAspect;
            uvRect.y = (1f - uvRect.height) * 0.5f;
        }
        else
        {
            uvRect.width = screenAspect / imageAspect;
            uvRect.x = (1f - uvRect.width) * 0.5f;
        }

        splashImage.uvRect = uvRect;
    }

    private static void SetBannersVisible(bool visible)
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IAdBannerProvider provider)
                provider.SetBannerVisible(visible);
        }
    }
}
