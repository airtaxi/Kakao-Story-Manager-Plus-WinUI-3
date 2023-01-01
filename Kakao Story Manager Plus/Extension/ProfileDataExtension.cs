using static KSMP.ApiHandler.DataType;
using static KSMP.ClassManager;

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
    
    private static FriendProfile GetFriendDataFromProfile(dynamic user, PostInformationMetadata metadata = null)
    {
        return new()
        {
            IsFavorite = user.is_favorite,
            Id = user.id,
            Name = user.display_name,
            Relationship = user.relationship,
            Metadata = metadata ?? new(),
            IsBirthday = user.is_birthday,
            ProfileUrl = GetUserProfileImage(user)
        };
    }
    
    public static FriendProfile GetFriendData(this UserProfile.ProfileData user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
    public static FriendProfile GetFriendData(this ProfileData.Profile user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
    public static FriendProfile GetFriendData(this FriendData.Profile user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
    public static FriendProfile GetFriendData(this CommentData.Actor user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
    public static FriendProfile GetFriendData(this CommentData.Writer user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
    public static FriendProfile GetFriendData(this ShareData.Actor user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
    public static FriendProfile GetFriendData(this CommentLikes.Actor user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
    public static FriendProfile GetFriendData(this Actor user, PostInformationMetadata metadata = null) => GetFriendDataFromProfile(user, metadata);
}
