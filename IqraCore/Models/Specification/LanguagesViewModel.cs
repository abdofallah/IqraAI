using IqraCore.Entities.Languages;

namespace IqraCore.Models.Specification
{
    public class LanguagesViewModel
    {
        public string Id { get; set; } = "";

        public string LocaleName { get; set; } = "";
        public string Name { get; set; } = "";

        public DateTime? DisabledAt { get; set; } = null;
        public string? PublicDisabledReason { get; set; } = null;

        public static LanguagesViewModel BuildModelFromEntity(LanguagesData entity)
        {
            return new LanguagesViewModel()
            {
                Id = entity.Id,
                LocaleName = entity.LocaleName,
                Name = entity.Name,
                DisabledAt = entity.DisabledAt,
                PublicDisabledReason = entity.PublicDisabledReason
            };
        }
    }
}
