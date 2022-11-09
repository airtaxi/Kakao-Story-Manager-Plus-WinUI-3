using KSMP.Utils;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.DevTools.V104.DOM;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing.Workflow;
using Windows.Security.Authentication.OnlineId;
using static KSMP.ApiHandler.DataType.FriendData;
using static KSMP.ClassManager;

namespace KSMP
{
    public class UserTagManager
    {
        private UserTag UserTag;
        private string UserId;
        private static bool initialized = false;

        private static ConcurrentBag<UserTagManager> s_instances = new();

        private UserTagManager(UserTag userTag)
        {
            UserTag = userTag;
            UserId = userTag.UserId;
        }
        private static UserTagManager GenerateUserTagManager(UserTag userTag)
        {
            var instance = new UserTagManager(userTag);
            s_instances.Add(instance);
            return instance;
        }

        public void AddNicknameHistory(string nickname)
        {
            UserTag.NicknameHistory.Add(nickname);
            SaveCurrentUserTags();
        }

        public void SetMemo(string memo)
        {
            UserTag.Memo = memo;
            SaveCurrentUserTags();
        }

        public void SetCustomNickname(string customNickname)
        {
            UserTag.CustomNickname = customNickname;
            SaveCurrentUserTags();
        }

        public List<string> GetNicknameHistory() => UserTag.NicknameHistory;
        public string GetMemo() => UserTag.Memo;
        public string GetCustomNickname() => UserTag.CustomNickname;

        public static async Task InitializeAsync(List<Profile> profiles)
        {
            var savedUserTags = GetSavedUserTags();

            await Task.Run(() =>
            {
                Parallel.ForEach(profiles, (profile) =>
                {
                    var userTag = savedUserTags.FirstOrDefault(x => x.UserId == profile.id) ?? new()
                    {
                        UserId = profile.id
                    };

                    // Check NicknameHistory
                    var nicknameHistory = userTag.NicknameHistory;
                    var nickname = profile.display_name;
                    if (!nicknameHistory.Contains(nickname)) nicknameHistory.Add(nickname);

                    GenerateUserTagManager(userTag);
                });
            });
            SaveCurrentUserTags();
            initialized = true;
        }
        
        private static void SaveCurrentUserTags()
        {
            var userTags = GetUserTags();
            Configuration.SetValue("UserTags", userTags);
        }
        private static List<UserTag> GetUserTags() => s_instances.Select(x => x.UserTag).ToList();
        private static List<UserTag> GetSavedUserTags() => (Configuration.GetValue("UserTags") as JArray)?.ToObject<List<UserTag>>() ?? new();

        public static UserTagManager GetUserTagManager(string userId)
        {
            if (!initialized) throw new Exception("Not Initialized!");

            var instance = s_instances.FirstOrDefault(x => x.UserId == userId);
            if (instance == null)
            {
                instance = GenerateUserTagManager(new UserTag() { UserId = userId });
                SaveCurrentUserTags();
            }

            return instance;
        }
    }

    public class DuplicateInstanceException : Exception { }
}
