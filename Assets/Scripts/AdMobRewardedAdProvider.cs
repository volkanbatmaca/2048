using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class AdMobRewardedAdProvider : MonoBehaviour, IRewardedAdProvider, IPrivacyOptionsProvider, IAdBannerProvider
{
    private const string AndroidRewardedTestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    private const string IOSRewardedTestAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    private const string AndroidRewardedAdUnitId = "ca-app-pub-1755657809365069/4803510701";
    private const string IOSRewardedAdUnitId = "ca-app-pub-1755657809365069/5266545136";
    private const string AndroidBannerTestAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    private const string IOSBannerTestAdUnitId = "ca-app-pub-3940256099942544/4411468910";
    private const string AndroidBannerAdUnitId = "ca-app-pub-1755657809365069/7768655204";
    private const string IOSBannerAdUnitId = "ca-app-pub-1755657809365069/7848546255";
    private const float StandardBannerHeightDp = 50f;

    private RewardedAd rewardedAd;
    private BannerView bannerView;
    private Action<bool> pendingRewardCallback;
    private bool adsInitialized;
    private bool initializationStarted;
    private bool isLoadingAd;
    private bool isShowingAd;
    private bool rewardEarned;
    private bool bannerShouldBeVisible;
    private bool bannerLoadStarted;

    public bool IsPrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
    public float ReservedBannerHeightPixels => GetBannerAdUnitId() == null
        ? 0f
        : StandardBannerHeightDp * (Screen.dpi > 0f ? Screen.dpi / 160f : 1f) + 8f;

    private void Awake()
    {
        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            MaxAdContentRating = MaxAdContentRating.G
        });
        RequestConsent();
    }

    private void OnDestroy()
    {
        DestroyRewardedAd();
        DestroyBannerAd();
        pendingRewardCallback = null;
        isShowingAd = false;
    }

    public void SetBannerVisible(bool visible)
    {
        if (bannerShouldBeVisible == visible)
            return;

        bannerShouldBeVisible = visible;
        if (bannerView == null)
            return;

        if (visible)
            bannerView.Show();
        else
            bannerView.Hide();
    }

    public void ShowRewardedAd(Action<bool> onCompleted)
    {
        if (onCompleted == null)
            throw new ArgumentNullException(nameof(onCompleted));

        if (isShowingAd || pendingRewardCallback != null)
        {
            onCompleted(false);
            return;
        }

        pendingRewardCallback = onCompleted;
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            ShowLoadedAd();
            return;
        }

        LoadRewardedAd();
    }

    public void ShowPrivacyOptions()
    {
        ConsentForm.ShowPrivacyOptionsForm(error =>
        {
            if (error != null)
                Debug.LogError("Gizlilik seçenekleri açılamadı: " + error.Message);
        });
    }

    private void RequestConsent()
    {
        ConsentInformation.Update(new ConsentRequestParameters(), error =>
        {
            if (error != null)
                Debug.LogError("Reklam izni bilgileri güncellenemedi: " + error.Message);

            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null)
                    Debug.LogError("Gizlilik formu yüklenemedi veya gösterilemedi: " + formError.Message);

                if (ConsentInformation.CanRequestAds())
                    InitializeAdsIfAllowed();
                else
                    CompletePendingReward(false);
            });

            InitializeAdsIfAllowed();
        });

        InitializeAdsIfAllowed();
    }

    private void InitializeAdsIfAllowed()
    {
        if (initializationStarted || !ConsentInformation.CanRequestAds())
            return;

        initializationStarted = true;
        MobileAds.Initialize(_ =>
        {
            adsInitialized = true;
            LoadBannerAd();
            LoadRewardedAd();
        });
    }

    private void LoadBannerAd()
    {
        if (!adsInitialized || bannerLoadStarted)
            return;

        var adUnitId = GetBannerAdUnitId();
        if (string.IsNullOrEmpty(adUnitId))
            return;

        bannerLoadStarted = true;
        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
        bannerView.OnBannerAdLoaded += () =>
        {
            if (bannerView != null && bannerShouldBeVisible)
                bannerView.Show();
        };
        bannerView.OnBannerAdLoadFailed += error =>
            Debug.LogWarning("Banner reklam yüklenemedi: " + error.GetMessage());
        bannerView.LoadAd(new AdRequest());
    }

    private void DestroyBannerAd()
    {
        if (bannerView == null)
            return;

        bannerView.Destroy();
        bannerView = null;
    }

    private void LoadRewardedAd()
    {
        if (!adsInitialized || isLoadingAd || rewardedAd != null)
            return;

        var adUnitId = GetRewardedAdUnitId();
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogError("Bu platform için ödüllü reklam birimi tanımlanmamış.");
            CompletePendingReward(false);
            return;
        }

        isLoadingAd = true;
        RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            isLoadingAd = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("Ödüllü reklam yüklenemedi: " + (error != null ? error.GetMessage() : "Reklam nesnesi boş."));
                CompletePendingReward(false);
                return;
            }

            rewardedAd = ad;
            RegisterAdEvents(ad);
            if (pendingRewardCallback != null)
                ShowLoadedAd();
        });
    }

    private void RegisterAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            if (rewardedAd != ad)
                return;

            DestroyRewardedAd();
            isShowingAd = false;
            var earnedReward = rewardEarned;
            rewardEarned = false;
            CompletePendingReward(earnedReward);
            LoadRewardedAd();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            if (rewardedAd != ad)
                return;

            Debug.LogError("Ödüllü reklam gösterilemedi: " + error.GetMessage());
            DestroyRewardedAd();
            isShowingAd = false;
            rewardEarned = false;
            CompletePendingReward(false);
            LoadRewardedAd();
        };
    }

    private void ShowLoadedAd()
    {
        if (rewardedAd == null || !rewardedAd.CanShowAd() || pendingRewardCallback == null)
        {
            CompletePendingReward(false);
            return;
        }

        rewardEarned = false;
        isShowingAd = true;
        rewardedAd.Show(_ => rewardEarned = true);
    }

    private void CompletePendingReward(bool earnedReward)
    {
        var callback = pendingRewardCallback;
        pendingRewardCallback = null;
        callback?.Invoke(earnedReward);
    }

    private void DestroyRewardedAd()
    {
        if (rewardedAd == null)
            return;

        rewardedAd.Destroy();
        rewardedAd = null;
    }

    private static string GetRewardedAdUnitId()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        #if UNITY_IOS && !UNITY_EDITOR
        return IOSRewardedTestAdUnitId;
        #else
        return AndroidRewardedTestAdUnitId;
        #endif
#elif UNITY_ANDROID
        return AndroidRewardedAdUnitId;
#elif UNITY_IOS
        return IOSRewardedAdUnitId;
#else
        return null;
#endif
    }

    private static string GetBannerAdUnitId()
    {
#if UNITY_EDITOR
        return null;
#elif UNITY_ANDROID && DEVELOPMENT_BUILD
        return AndroidBannerTestAdUnitId;
#elif UNITY_IOS && DEVELOPMENT_BUILD
        return IOSBannerTestAdUnitId;
#elif UNITY_ANDROID
        return AndroidBannerAdUnitId;
#elif UNITY_IOS
        return IOSBannerAdUnitId;
#else
        return null;
#endif
    }
}
