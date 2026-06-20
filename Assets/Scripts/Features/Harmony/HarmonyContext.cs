using TimeAura.Features.Data;
using TimeAura.Features.Social;

namespace TimeAura.Features.Harmony
{
    /// <summary>
    /// Context data passed to the Harmony Channel when a session is opened.
    /// Contains information about the partner and the request they are responding to.
    /// </summary>
    public class HarmonyContext
    {
        public UserProfile PartnerProfile { get; set; }
        public Post RelatedPost { get; set; }
        
        public HarmonyContext(UserProfile partnerProfile, Post relatedPost = null)
        {
            PartnerProfile = partnerProfile;
            RelatedPost = relatedPost;
        }
    }
}
