using System;
using UnityEngine;

namespace TimeAura.Features.Social
{
    /// <summary>Type of post in the feed.</summary>
    public enum PostType
    {
        Chronicle,      // Regular narrative post
        ServiceRequest  // Concrete request for a service
    }

    /// <summary>Represents the economic realm of exchange.</summary>
    public enum ExchangeRealm
    {
        None,
        Ether,
        Matter
    }

    /// <summary>Pillars / service categories.</summary>
    public enum ServiceCategory
    {
        All,
        Teaching,   // Навчання / Мудрість
        Craft,      // Ремесло / Дім
        Code,       // Ефірний Код (IT)
        Art,        // Мистецтво / Слово
        Nature      // Природа / Сад
    }

    /// <summary>Horas price type.</summary>
    public enum PriceType
    {
        Free,
        Fixed,
        Negotiate
    }

    /// <summary>
    /// Social post/feed item model
    /// </summary>
    [Serializable]
    public class Post
    {
        public string postId;
        public string userId;
        public string username;
        public string userAvatarUrl;

        public string content;
        public string[] imageUrls;
        public string videoUrl;

        public int likesCount;
        public int commentsCount;
        public int sharesCount;

        public bool isLiked;
        public bool isBookmarked;

        public DateTime createdAt;
        public DateTime? editedAt;

        // ── Service Request fields ──────────────────────────────
        public PostType postType = PostType.Chronicle;
        public ServiceCategory serviceCategory = ServiceCategory.All;
        public long horasPrice;          // 0 = see priceType
        public PriceType priceType = PriceType.Fixed;
        public float distanceKm;          // distance from current user
        public bool isUrgent;             // posted within last 24 h highlight
        public string authorCity;         // location label
        public ExchangeRealm realm = ExchangeRealm.Ether;
        public long priceAtoms;            // stored in db
        public long priceWaves;            // stored in db

        // Client-side cache
        [NonSerialized] public Texture2D[] cachedImages;
        [NonSerialized] public bool areImagesLoading;
    }


    /// <summary>
    /// API request/response models
    /// </summary>
    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class LoginResponse
    {
        public string token;
        public Data.UserProfile user;
    }

    [Serializable]
    public class FeedRequest
    {
        public int page = 1;
        public int pageSize = 20;
        public string userId; // For user-specific feed
    }

    [Serializable]
    public class FeedResponse
    {
        public Post[] posts;
        public int totalCount;
        public bool hasMore;
    }

    [Serializable]
    public class CreatePostRequest
    {
        public string content;
        public string[] imageUrls;
    }

    [Serializable]
    public class CreatePostResponse
    {
        public Post post;
    }
}
