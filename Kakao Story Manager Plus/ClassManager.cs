﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Net;
using static KSMP.ClassManager;

namespace KSMP;

public class ClassManager
{
    public class UserTag
    {
        public string UserId { get; set; } // From KakaoStory
        public List<string> NicknameHistory { get; set; } = new();
        public string CustomNickname { get; set; }
        public string Memo { get; set; }
    }

    public class RestartFlag
    {
        public List<Cookie> Cookies { get; set; }
        public string LastArgs { get; set; }
        public string LastFeedId { get; set; }
        public bool WasMaximized { get; set; }
        public bool ShowTimeline { get; set; }
    }

    public enum PostWritingPermission
    {
        F,
        A,
        M
    };

    public class PostInformationMetadata
    {
        public object Tag { get; set; }
        public UIElement Control { get; set; }
        public Flyout Flyout { get; set; }
        public bool IsUp { get; set; }
    }
}

public class FriendProfile
{
    public string Id { get; set; }
    public string Relationship { get; set; }
    public string ProfileUrl { get; set; }
    public string Name { get; set; }
    public bool IsBirthday { get; set; }
    public bool IsFavorite { get; set; }

    public PostInformationMetadata Metadata { get; set; } = new();
    public Visibility BirthdayVisiblity => IsBirthday ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FavoriteVisiblity => IsFavorite ? Visibility.Visible : Visibility.Collapsed;
}
