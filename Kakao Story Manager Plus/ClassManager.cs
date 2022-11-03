using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Net;

namespace KSMP;

public class ClassManager
{
    public class RestartFlag
    {
        public List<Cookie> Cookies { get; set; }
        public string LastArgs { get; set; }
        public string PostId { get; set; }
        public string LastFeedId { get; set; }
        public bool WasMaximized { get; set; }
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

    public class FriendProfile
    {
        public string Id { get; set; }
        public string Relationship { get; set; }
        public string ProfileUrl { get; set; }
        public string Name { get; set; }
        public PostInformationMetadata Metadata { get; set; } = new();
    }
}
