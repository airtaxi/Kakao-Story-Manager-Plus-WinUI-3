using static StoryApi.ApiHandler.DataType;

namespace KSMP.Extension;

public static class ProfileDataExtension
{
    private static string GetUserProfileImage(dynamic user)
    {
        bool willUseGifProfileImage = (Utils.Configuration.GetValue("UseGifProfileImage") as bool?) ?? true;
        var url = willUseGifProfileImage ?
            user.profile_video_url_square_small ?? user.profile_thumbnail_url :
            user.profile_thumbnail_url;
        return url;
    }

    public static string GetValidUserProfileUrl(this UserProfile.ProfileData user) => GetUserProfileImage(user);
    public static string GetValidUserProfileUrl(this ProfileData.Profile user) => GetUserProfileImage(user);
    public static string GetValidUserProfileUrl(this FriendData.Profile user) => GetUserProfileImage(user);
    public static string GetValidUserProfileUrl(this CommentData.Actor user) => GetUserProfileImage(user);
    public static string GetValidUserProfileUrl(this CommentData.Writer user) => GetUserProfileImage(user);
    public static string GetValidUserProfileUrl(this ShareData.Actor user) => GetUserProfileImage(user);
    public static string GetValidUserProfileUrl(this CommentLikes.Actor user) => GetUserProfileImage(user);
    public static string GetValidUserProfileUrl(this Actor user) => GetUserProfileImage(user);
}
