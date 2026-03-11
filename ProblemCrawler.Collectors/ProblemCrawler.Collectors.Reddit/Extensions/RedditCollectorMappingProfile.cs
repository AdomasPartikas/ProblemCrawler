using AutoMapper;
using ProblemCrawler.Core.Constants;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Core.Models.Reddit;

namespace ProblemCrawler.Collectors.Reddit.Extensions;

public class RedditCollectorMappingProfile : Profile
{
    public RedditCollectorMappingProfile()
    {
        CreateMap<RedditPost, CollectorItem>()
            .ForMember(dest => dest.ItemType, opt => opt.MapFrom(_ => "Post"))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(_ => "Reddit"))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.IsSelf ? src.Selftext ?? string.Empty : src.Url ?? string.Empty))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds((long)src.CreatedUtc).UtcDateTime))
            .ForMember(dest => dest.SourceUrl, opt => opt.MapFrom(src => src.Permalink != null ? $"{RedditCollectorDefaults.BaseUrl}{src.Permalink}" : null))
            .ForMember(dest => dest.Metadata, opt => opt.Ignore())
            .AfterMap((src, dest) => dest.Metadata = BuildPostMetadata(src));

        CreateMap<RedditComment, CollectorItem>()
            .ForMember(dest => dest.ItemType, opt => opt.MapFrom(_ => "Comment"))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(_ => "Reddit"))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Body))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds((long)src.CreatedUtc).UtcDateTime))
            .ForMember(dest => dest.SourceUrl, opt => opt.MapFrom(src => src.Permalink != null ? $"{RedditCollectorDefaults.BaseUrl}{src.Permalink}" : null))
            .ForMember(dest => dest.Metadata, opt => opt.Ignore())
            .AfterMap((src, dest) => dest.Metadata = BuildCommentMetadata(src));
    }

    private static Dictionary<string, object?> BuildPostMetadata(RedditPost src)
    {
        return new Dictionary<string, object?>
        {
            ["Title"] = src.Title,
            ["Score"] = src.Score,
            ["Ups"] = src.Ups,
            ["Downs"] = src.Downs,
            ["UpvoteRatio"] = src.UpvoteRatio,
            ["NumComments"] = src.NumComments,
            ["Subreddit"] = src.Subreddit,
            ["SubredditSubscribers"] = src.SubredditSubscribers,
            ["IsStickied"] = src.Stickied,
            ["IsPinned"] = src.Pinned,
            ["IsArchived"] = src.Archived,
            ["IsLocked"] = src.Locked,
            ["FlairText"] = src.LinkFlairText,
            ["RawPost"] = src,
        };
    }

    private static Dictionary<string, object?> BuildCommentMetadata(RedditComment src)
    {
        return new Dictionary<string, object?>
        {
            ["Score"] = src.Score,
            ["Ups"] = src.Ups,
            ["Downs"] = src.Downs,
            ["ScoreHidden"] = src.ScoreHidden,
            ["Depth"] = src.Depth,
            ["IsSubmitter"] = src.IsSubmitter,
            ["ParentId"] = src.ParentId,
            ["LinkId"] = src.LinkId,
            ["Subreddit"] = src.Subreddit,
            ["IsArchived"] = src.Archived,
            ["IsLocked"] = src.Locked,
            ["Distinguished"] = src.Distinguished,
            ["IsPremium"] = src.AuthorPremium,
            ["RawComment"] = src,
        };
    }
}