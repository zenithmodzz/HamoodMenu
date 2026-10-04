using System;
using System.Collections;
using Seralyth.Menu;
using UnityEngine;
using UnityEngine.Networking;

namespace Seralyth.Managers
{
    internal static class UpdateChecker
    {
        private const string VersionUrl = "https://raw.githubusercontent.com/zenithmodzz/auto-update/main/version.txt";
        private const string RepositoryUrl = "https://github.com/zenithmodzz/auto-update";

        public static IEnumerator CheckForUpdates()
        {
            yield return new WaitForSeconds(3f);

            using UnityWebRequest request = UnityWebRequest.Get(VersionUrl);
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                LogManager.LogWarning($"Failed to check for menu updates: {request.error}");
                yield break;
            }

            string latestVersionText = request.downloadHandler.text.Trim().TrimStart('v', 'V');
            if (!Version.TryParse(latestVersionText, out Version latestVersion))
            {
                LogManager.LogError($"Invalid latest menu version from {VersionUrl}: {request.downloadHandler.text}");
                yield break;
            }

            if (!Version.TryParse(PluginInfo.Version, out Version currentVersion))
            {
                LogManager.LogError($"Invalid current menu version: {PluginInfo.Version}");
                yield break;
            }

            if (latestVersion <= currentVersion)
                yield break;

            LogManager.LogWarning($"Menu v{currentVersion} is outdated; latest version is v{latestVersion}.");
            NotificationManager.SendNotification("Menu is outdated! Opening GitHub…", 10000);
            Application.OpenURL(RepositoryUrl);
        }
    }
}
