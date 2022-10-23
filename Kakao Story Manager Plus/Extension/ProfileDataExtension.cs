namespace KSMP.Extension;

public static class ProfileDataExtension
{
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.UserProfile.ProfileData user) => user.profile_thumbnail_url;
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.ProfileData.Profile user) => user.profile_thumbnail_url;
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.FriendData.Profile user) => user.profile_thumbnail_url;
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.CommentData.Actor user) => user.profile_thumbnail_url;
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.CommentData.Writer user) => user.profile_thumbnail_url;
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.ShareData.Actor user) => user.profile_thumbnail_url;
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.CommentLikes.Actor user) => user.profile_thumbnail_url;
    //public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.Actor user) => user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.UserProfile.ProfileData user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.ProfileData.Profile user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.FriendData.Profile user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.CommentData.Actor user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.CommentData.Writer user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.ShareData.Actor user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.CommentLikes.Actor user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
    public static string GetValidUserProfileUrl(this StoryApi.ApiHandler.DataType.Actor user) => user.profile_video_url_square_small ?? user.profile_thumbnail_url;
}
