using BepInEx.Bootstrap;
using HarmonyLib;
using System.Collections.Generic;
using TootTallyCore.APIServices;
using TootTallyCore.Utils.TootTallyNotifs;
using UnityEngine;

namespace TootTallyAccounts
{
    public static class TootTallyUser
    {
        public static SerializableClass.User userInfo = new SerializableClass.User() { allowSubmit = false, id = 0, username = "Guest" };
        private static List<SerializableClass.Message> _messagesReceived;
        private static TootTallyLoginPanel _loginPanel;
        private static bool _hasGreetedUser;

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
        [HarmonyPostfix]
        public static void InitializeUser()
        {
            _messagesReceived ??= new List<SerializableClass.Message>();
            if ((Plugin.GetAPIKey == Plugin.GetAPIKey || Plugin.GetAPIKey == "") && Plugin.Instance.option.ShowLoginPanel.Value)
            {
                if (Plugin.Instance.option.ShowLoginPanel.Value)
                    OpenLoginPanel();
            }
            else if (userInfo.id == 0 && Plugin.GetAPIKey != Plugin.DEFAULT_APIKEY)
            {
                Plugin.Instance.StartCoroutine(TootTallyAPIService.GetUserFromAPIKey(Plugin.GetAPIKey, user =>
                {
                    OnUserLogin(user);
                    if (user.id == 0 && Plugin.Instance.option.ShowLoginPanel.Value)
                        OpenLoginPanel();
                }));

                /*Plugin.Instance.StartCoroutine(ThunderstoreAPIService.GetMostRecentModVersion(version =>
                {
                    if (version.CompareTo(PluginInfo.PLUGIN_VERSION) > 0)
                        TootTallyNotifManager.DisplayNotif("New update available!\nNow available on Thunderstore", Color.yellow, 8.5f);
                }));*/
            }
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        public static void DisplayReceivedMessaged()
        {
            if (userInfo.id == 0) return; //Do not receive massages if not logged in

            if (!_hasGreetedUser)
            {
                TootTallyNotifManager.DisplayNotif($"Welcome, {userInfo.username}!", Color.white, 9f);
                _hasGreetedUser = true;
            }

            Plugin.Instance.StartCoroutine(TootTallyAPIService.GetMessageFromAPIKey(Plugin.GetAPIKey, messages =>
            {
                Plugin.LogInfo("Messages received: " + messages.results.Count);
                foreach (SerializableClass.Message message in messages.results)
                {
                    if (_messagesReceived.FindAll(m => m.sent_on == message.sent_on).Count == 0)
                    {
                        _messagesReceived.Add(message);
                        TootTallyNotifManager.DisplayNotif($"<size=14>From:{message.author} ({message.sent_on})</size>\n{message.message}", Color.white, 16f);
                    }
                }
            }));
        }

        public static void OnUserLogin(SerializableClass.User user)
        {
            userInfo = user;
            if (userInfo.api_key != null && userInfo.api_key != "")
                Plugin.Instance.option.APIKey.Value = userInfo.api_key;
            if (userInfo.id == 0)
            {
                userInfo.allowSubmit = false;
            }
            else
            {
                Plugin.Instance.StartCoroutine(TootTallyAPIService.SendModInfo(Plugin.Instance.option.APIKey.Value, Chainloader.PluginInfos, allowSubmit =>
                {
                    userInfo.allowSubmit = allowSubmit;
                }));
                UserStatusManager.SetUserStatus(UserStatusManager.UserStatus.Online);
            }
        }

        public static void OpenLoginPanel()
        {
            _loginPanel ??= new TootTallyLoginPanel();
            _loginPanel.Show();
        }
    }
}
