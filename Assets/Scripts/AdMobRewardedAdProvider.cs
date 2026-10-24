using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class AdMobRewardedAdProvider : MonoBehaviour, IRewardedAdProvider, IPrivacyOptionsProvider
{
    private const string AndroidRewardedTestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    private const string IOSRewardedTestAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    private const string AndroidRewardedAdUnitId = "ca-app-pub-1755657809365069/4803510701";
    private const string IOSRewardedAdUnitId = "ca-app-pub-1755657809365069/5266545136";

    private RewardedAd rewardedAd;
    private Action<bool> pendingRewardCallback;
    private bool adsInitialized;
    private bool initializationStarted;
    private bool isLoadingAd;
    private bool isShowingAd;

    public bool IsPrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    private void Awake()
    {
        RequestConsent();
    }

    private void OnDestroy()
    {
        DestroyRewardedAd();
        pendingRewardCallback = null;
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
            LoadRewardedAd();
        });
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
            CompletePendingReward(false);
            LoadRewardedAd();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            if (rewardedAd != ad)
                return;

            Debug.LogError("Ödüllü reklam gösterilemedi: " + error.GetMessage());
            DestroyRewardedAd();
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

        isShowingAd = true;
        rewardedAd.Show(_ => CompletePendingReward(true));
    }

    private void CompletePendingReward(bool earnedReward)
    {
        var callback = pendingRewardCallback;
        pendingRewardCallback = null;
        isShowingAd = false;
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
}
