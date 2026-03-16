using AutoMapper;
using ProblemCrawler.Core.Models;
using ProblemCrawler.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure.Profiles
{
    /// <summary>
    /// Defines AutoMapper configuration for mapping between collector item domain models and their corresponding entity
    /// representations.
    /// </summary>
    /// <remarks>This profile establishes bidirectional mappings between the CollectorItem and
    /// CollectorItemEntity types, configuring property mappings and ignoring properties as needed. Use this profile to
    /// enable seamless conversion between domain and persistence models when using AutoMapper.</remarks>
    public class ContentItemEntityMappingProfile : Profile
    {
        public ContentItemEntityMappingProfile()
        {
            CreateMap<CollectorItem, CollectorItemEntity>();
                

        }
    }
}
