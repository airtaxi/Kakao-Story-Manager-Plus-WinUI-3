using KSMP.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static KSMP.ApiHandler.DataType.FriendData;
using static KSMP.ClassManager;

namespace KSMP;

public class UserTagManager
{
    private UserTag _userTag;
    private string _userId;
    private static bool s_initialized = false;

    private static ConcurrentBag<UserTagManager> s_instances = new();

    private UserTagManager(UserTag userTag)
    {
        _userTag = userTag;
        _userId = userTag.UserId;
    }

    public void AddNicknameHistory(string nickname)
    {
        _userTag.NicknameHistory.Add(nickname);
        SaveCurrentUserTags();
    }

    public void SetMemo(string memo)
    {
        _userTag.Memo = memo;
        SaveCurrentUserTags();
    }

    public void SetCustomNickname(string customNickname)
    {
        _userTag.CustomNickname = customNickname;
        SaveCurrentUserTags();
    }

    public List<string> GetNicknameHistory() => _userTag.NicknameHistory;
    public string GetMemo() => _userTag.Memo;
    public string GetCustomNickname() => _userTag.CustomNickname;

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
        s_initialized = true;
    }


    private static UserTagManager GenerateUserTagManager(UserTag userTag)
    {
        var instance = new UserTagManager(userTag);
        s_instances.Add(instance);
        return instance;
    }

    private static void SaveCurrentUserTags()
    {
        var userTags = GetUserTags();
        Configuration.SetValue("UserTags", userTags);
    }

    private static List<UserTag> GetUserTags() => s_instances.Select(x => x._userTag).ToList();
    private static List<UserTag> GetSavedUserTags() => (Configuration.GetValue("UserTags") as JArray)?.ToObject<List<UserTag>>() ?? new();

    public static UserTagManager GetUserTagManager(string userId)
    {
        if (!s_initialized) throw new Exception("Not Initialized!");

        var instance = s_instances.FirstOrDefault(x => x._userId == userId);
        if (instance == null)
        {
            instance = GenerateUserTagManager(new UserTag() { UserId = userId });
            SaveCurrentUserTags();
        }

        return instance;
    }
}
